import { Parser } from "expr-eval";
import {
  CURRENT_USER_PREFIX,
  CURRENT_USER_PROPERTIES,
} from "@/components/ui/input-with-placeholders/current-user";
import type { FieldType } from "@/features/space/types";

/**
 * Context for expression evaluation
 */
export interface EvaluationContext {
  /** Raw input values as strings (key is variable name) */
  variables: Record<string, string>;
  /** Type definitions for each variable */
  types: Record<string, FieldType>;
  /** Current user context */
  currentUser: {
    email: string;
    id: string;
    firstName: string;
    lastName: string;
  };
}

/**
 * Result of expression evaluation
 */
export type EvaluationResult =
  | { success: true; value: string }
  | { success: false; error: string };

/**
 * Result of expression validation (syntax check only)
 */
export interface ValidationResult {
  isValid: boolean;
  errors: string[];
  referencedVariables: string[];
}

// Regex pattern strings (use with new RegExp() to avoid global state issues)
// Allow any content (including braces) inside {{ ... }} with a non-greedy match.
const EXPRESSION_PATTERN = "\\{\\{([\\s\\S]*?)\\}\\}";
// Match @"quoted name" or @identifier (with optional dot notation)
// Group 1: quoted name (without quotes), Group 2: unquoted identifier
const VARIABLE_PATTERN =
  '@(?:"([^"]+)"|([a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)?))';

// Create fresh regex instances to avoid global state issues
function createExpressionRegex(): RegExp {
  return new RegExp(EXPRESSION_PATTERN, "g");
}

function createVariableRegex(): RegExp {
  return new RegExp(VARIABLE_PATTERN, "g");
}

/**
 * Extract the variable name from a regex match (handles both quoted and unquoted)
 */
function getVariableNameFromMatch(match: RegExpMatchArray): string {
  // match[1] is quoted name, match[2] is unquoted identifier
  return match[1] ?? match[2];
}

/**
 * Normalize a variable name for use in expr-eval (replace special chars with underscores)
 */
function normalizeVariableName(name: string): string {
  // Replace dots and spaces with underscores for expr-eval compatibility
  return name.replace(/[\s.]+/g, "_");
}

function isJsonExpression(expression: string): boolean {
  const trimmed = expression.trim();
  return trimmed.startsWith("{") || trimmed.startsWith("[");
}

function isIndexInJsonString(expression: string, index: number): boolean {
  let inString = false;
  for (let i = 0; i < index; i += 1) {
    const char = expression[i];
    if (char === '"') {
      // Count preceding backslashes to determine if this quote is escaped.
      let backslashes = 0;
      for (let j = i - 1; j >= 0 && expression[j] === "\\"; j -= 1) {
        backslashes += 1;
      }
      if (backslashes % 2 === 0) {
        inString = !inString;
      }
    }
  }
  return inString;
}

/**
 * Transform JavaScript-style operators to expr-eval syntax
 * - && -> and
 * - || -> or
 * - ! -> not (when not followed by =)
 */
function transformOperators(expression: string): string {
  return expression
    .replace(/&&/g, " and ")
    .replace(/\|\|/g, " or ")
    .replace(/!(?!=)/g, "not ");
}

/**
 * Coerce a string value to the appropriate type for expression evaluation
 */
function coerceValue(
  value: string,
  type: FieldType | undefined
): string | number | boolean {
  if (type === "boolean") {
    return value.toLowerCase() === "true";
  }
  if (type === "integer") {
    const num = Number.parseInt(value, 10);
    return Number.isNaN(num) ? 0 : num;
  }
  // For text, email, date - keep as string
  return value;
}

function formatJsonValue(value: string, type: FieldType | undefined): string {
  if (type === "boolean") {
    return value.toLowerCase() === "true" ? "true" : "false";
  }
  if (type === "integer") {
    const num = Number.parseInt(value, 10);
    return Number.isNaN(num) ? "0" : String(num);
  }
  return JSON.stringify(value);
}

function replaceVariablesForJsonValidation(expression: string): {
  result: string;
  error?: string;
} {
  const varRegex = createVariableRegex();
  let error: string | undefined;
  const result = expression.replace(
    varRegex,
    (_match, quotedName, unquotedName, offset: number) => {
      if (error) {
        return _match;
      }
      if (isIndexInJsonString(expression, offset)) {
        error =
          "Variables cannot be used inside JSON strings; use @Var as a JSON value.";
        return _match;
      }
      const name = quotedName ?? unquotedName;
      if (!name) {
        return _match;
      }
      // Use a neutral JSON value for syntax validation.
      return "0";
    }
  );
  return { result, error };
}

function replaceVariablesInJsonExpression(
  expression: string,
  context: EvaluationContext
): { result?: string; error?: string } {
  const varRegex = createVariableRegex();
  let error: string | undefined;
  const result = expression.replace(
    varRegex,
    (_match, quotedName, unquotedName, offset: number) => {
      if (error) {
        return _match;
      }
      if (isIndexInJsonString(expression, offset)) {
        error =
          "Variables cannot be used inside JSON strings; use @Var as a JSON value.";
        return _match;
      }
      const name = quotedName ?? unquotedName;
      if (!name) {
        return _match;
      }

      let value: string | undefined;
      let type: FieldType | undefined;

      if (isCurrentUserVariable(name)) {
        value = getCurrentUserValue(name, context.currentUser);
        type = "text";
      } else {
        value = context.variables[name];
        type = context.types[name];
      }

      if (value === undefined) {
        error = `Variable '${name}' has no value`;
        return _match;
      }

      return formatJsonValue(value, type);
    }
  );

  return { result, error };
}

/**
 * Extract variable names referenced in an expression (prefixed with @)
 */
export function extractVariables(expression: string): string[] {
  const variables: string[] = [];
  const regex = createVariableRegex();
  for (const match of expression.matchAll(regex)) {
    const varName = getVariableNameFromMatch(match);
    if (!variables.includes(varName)) {
      variables.push(varName);
    }
  }
  return variables;
}

/**
 * Check if a variable name is a current_user property
 */
function isCurrentUserVariable(varName: string): boolean {
  return varName.startsWith(`${CURRENT_USER_PREFIX}.`);
}

/**
 * Get current user value by property name
 */
function getCurrentUserValue(
  varName: string,
  currentUser: EvaluationContext["currentUser"]
): string | undefined {
  if (!isCurrentUserVariable(varName)) {
    return undefined;
  }
  const property = varName.slice(CURRENT_USER_PREFIX.length + 1);
  const propInfo = CURRENT_USER_PROPERTIES.find((p) => p.key === property);
  if (!propInfo) {
    return undefined;
  }
  return currentUser[propInfo.key as keyof typeof currentUser];
}

/**
 * Validate an expression's syntax and variable references
 */
export function validateExpression(
  expression: string,
  availableVariables: string[]
): ValidationResult {
  const errors: string[] = [];
  const referencedVariables = extractVariables(expression);

  // Check for unknown variables
  for (const varName of referencedVariables) {
    if (isCurrentUserVariable(varName)) {
      // Validate current_user property
      const property = varName.slice(CURRENT_USER_PREFIX.length + 1);
      const isValid = CURRENT_USER_PROPERTIES.some((p) => p.key === property);
      if (!isValid) {
        errors.push(`Unknown current_user property: ${property}`);
      }
    } else if (!availableVariables.includes(varName)) {
      errors.push(`Unknown variable: ${varName}`);
    }
  }

  // Try to parse the expression (syntax validation)
  if (isJsonExpression(expression)) {
    const { result, error } = replaceVariablesForJsonValidation(expression);
    if (error) {
      errors.push(error);
    } else {
      try {
        JSON.parse(result);
      } catch (e) {
        errors.push(
          `JSON syntax error: ${e instanceof Error ? e.message : String(e)}`
        );
      }
    }
  } else {
    try {
      // Replace @Variables with placeholder identifiers for parsing
      const varRegex = createVariableRegex();
      let normalizedExpr = expression.replace(
        varRegex,
        (_match, quotedName, unquotedName) => {
          const name = quotedName ?? unquotedName;
          return normalizeVariableName(name);
        }
      );
      // Transform JS operators to expr-eval syntax
      normalizedExpr = transformOperators(normalizedExpr);
      const parser = new Parser();
      parser.parse(normalizedExpr);
    } catch (e) {
      errors.push(
        `Syntax error: ${e instanceof Error ? e.message : String(e)}`
      );
    }
  }

  return {
    isValid: errors.length === 0,
    errors,
    referencedVariables,
  };
}

/**
 * Evaluate a single expression (content between {{ }})
 */
export function evaluateExpression(
  expression: string,
  context: EvaluationContext
): EvaluationResult {
  if (isJsonExpression(expression)) {
    const { result, error } = replaceVariablesInJsonExpression(
      expression,
      context
    );
    if (error || !result) {
      return {
        success: false,
        error: error ?? "Invalid JSON expression",
      };
    }
    try {
      const parsed = JSON.parse(result);
      return {
        success: true,
        value: JSON.stringify(parsed),
      };
    } catch (e) {
      return {
        success: false,
        error: `JSON syntax error: ${e instanceof Error ? e.message : String(e)}`,
      };
    }
  }

  try {
    const parser = new Parser();

    // Build the variable scope with coerced values
    const scope: Record<string, string | number | boolean> = {};

    // Extract all @Variables from the expression
    const referencedVars = extractVariables(expression);

    for (const varName of referencedVars) {
      let value: string | undefined;
      let type: FieldType | undefined;

      if (isCurrentUserVariable(varName)) {
        value = getCurrentUserValue(varName, context.currentUser);
        type = "text"; // current_user properties are always strings
      } else {
        value = context.variables[varName];
        type = context.types[varName];
      }

      if (value === undefined) {
        return {
          success: false,
          error: `Variable '${varName}' has no value`,
        };
      }

      // Normalize variable name for expr-eval (replace dots and spaces with _)
      const normalizedName = normalizeVariableName(varName);
      scope[normalizedName] = coerceValue(value, type);
    }

    // Replace @Variables with normalized identifiers for expr-eval
    const varRegex = createVariableRegex();
    let normalizedExpr = expression.replace(
      varRegex,
      (_match, quotedName, unquotedName) => {
        const name = quotedName ?? unquotedName;
        return normalizeVariableName(name);
      }
    );
    // Transform JS operators to expr-eval syntax
    normalizedExpr = transformOperators(normalizedExpr);

    // Parse and evaluate
    const parsed = parser.parse(normalizedExpr);
    const result = parsed.evaluate(scope);

    // Convert result back to string
    return {
      success: true,
      value: String(result),
    };
  } catch (e) {
    return {
      success: false,
      error: e instanceof Error ? e.message : String(e),
    };
  }
}

/**
 * Process a template string, evaluating all {{ expression }} patterns
 * Returns the processed string with expressions replaced by their values
 */
export function processTemplate(
  template: string,
  context: EvaluationContext
): EvaluationResult {
  let result = template;
  let lastError: string | null = null;

  // Find and replace all {{ expression }} patterns
  const regex = createExpressionRegex();
  const matches = [...template.matchAll(regex)];

  for (const match of matches) {
    const fullMatch = match[0];
    const expression = match[1].trim();

    const evalResult = evaluateExpression(expression, context);
    if (!evalResult.success) {
      lastError = evalResult.error;
      // Keep the original expression in place on error
      continue;
    }

    result = result.replace(fullMatch, evalResult.value);
  }

  if (lastError) {
    return { success: false, error: lastError };
  }

  return { success: true, value: result };
}

/**
 * Check if a string contains any {{ expression }} patterns
 */
export function hasExpressions(value: string): boolean {
  const regex = createExpressionRegex();
  return regex.test(value);
}

/**
 * Extract all expressions from a template string
 */
export function extractExpressions(template: string): string[] {
  const expressions: string[] = [];
  const regex = createExpressionRegex();
  for (const match of template.matchAll(regex)) {
    expressions.push(match[1].trim());
  }
  return expressions;
}

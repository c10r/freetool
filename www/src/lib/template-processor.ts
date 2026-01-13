import type { FieldType } from "@/features/space/types";
import {
  type EvaluationContext,
  hasExpressions,
  processTemplate,
} from "./expression-evaluator";

/**
 * Process all templates in an app configuration, evaluating expressions.
 * This can be used to preview what the final request will look like.
 */
export interface AppTemplates {
  urlPath?: string | null;
  urlParameters?: Array<{ key: string; value: string }>;
  headers?: Array<{ key: string; value: string }>;
  body?: Array<{ key: string; value: string }>;
}

export interface ProcessedTemplates {
  urlPath: string | null;
  urlParameters: Array<{ key: string; value: string }>;
  headers: Array<{ key: string; value: string }>;
  body: Array<{ key: string; value: string }>;
  errors: string[];
}

/**
 * Process all templates in app configuration, evaluating any {{ expression }} patterns.
 * Returns the processed templates with expressions evaluated, and any errors encountered.
 */
export function processAppTemplates(
  templates: AppTemplates,
  context: EvaluationContext
): ProcessedTemplates {
  const errors: string[] = [];

  const processValue = (value: string | null | undefined): string | null => {
    if (!value) {
      return null;
    }
    if (!hasExpressions(value)) {
      return value;
    }

    const result = processTemplate(value, context);
    if (!result.success) {
      errors.push(result.error);
      return value; // Return original on error
    }
    return result.value;
  };

  const processKeyValuePairs = (
    pairs: Array<{ key: string; value: string }> | undefined | null
  ): Array<{ key: string; value: string }> => {
    if (!pairs) {
      return [];
    }
    return pairs.map((pair) => ({
      key: processValue(pair.key) || pair.key,
      value: processValue(pair.value) || pair.value,
    }));
  };

  return {
    urlPath: processValue(templates.urlPath),
    urlParameters: processKeyValuePairs(templates.urlParameters),
    headers: processKeyValuePairs(templates.headers),
    body: processKeyValuePairs(templates.body),
    errors,
  };
}

/**
 * Build evaluation context from input values and types
 */
export function buildEvaluationContext(
  inputValues: Record<string, string>,
  inputTypes: Record<string, FieldType>,
  currentUser: EvaluationContext["currentUser"]
): EvaluationContext {
  return {
    variables: inputValues,
    types: inputTypes,
    currentUser,
  };
}

/**
 * Check if any template in the app configuration contains expressions
 */
export function hasAnyExpressions(templates: AppTemplates): boolean {
  const checkValue = (value: string | null | undefined): boolean =>
    !!value && hasExpressions(value);

  const checkPairs = (
    pairs: Array<{ key: string; value: string }> | undefined | null
  ): boolean => {
    if (!pairs) {
      return false;
    }
    return pairs.some((pair) => checkValue(pair.key) || checkValue(pair.value));
  };

  return (
    checkValue(templates.urlPath) ||
    checkPairs(templates.urlParameters) ||
    checkPairs(templates.headers) ||
    checkPairs(templates.body)
  );
}

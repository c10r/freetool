import {
  $createParagraphNode,
  $createTextNode,
  $getRoot,
  $isTextNode,
} from "lexical";
import { validateExpression } from "@/lib/expression-evaluator";
import type { AppInput } from "../input-with-placeholders.types";
import {
  isCurrentUserPlaceholder,
  isValidCurrentUserPlaceholder,
} from "./current-user";
import { $createExpressionNode, $isExpressionNode } from "./ExpressionNode";
import { $createPlaceholderNode, $isPlaceholderNode } from "./PlaceholderNode";

// Match both {{ expression }} and @placeholder patterns
// Expressions first (longer pattern takes priority)
const EXPRESSION_REGEX = /\{\{([^{}]+)\}\}/g;
// Match @"quoted name" or @variableName (identifier with optional dot notation)
// Group 1: quoted name (without quotes), Group 2: unquoted identifier
const PLACEHOLDER_REGEX =
  /@(?:"([^"]+)"|([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?))/g;

/**
 * Check if a variable name needs to be quoted when serializing
 */
function needsQuoting(name: string): boolean {
  // Quote if contains spaces or doesn't match simple identifier pattern
  return /\s/.test(name) || !/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(name);
}

interface ParsedToken {
  type: "text" | "placeholder" | "expression";
  content: string;
  start: number;
  end: number;
}

/**
 * Tokenize a string into text, placeholder, and expression segments
 */
function tokenize(value: string): ParsedToken[] {
  const tokens: ParsedToken[] = [];
  const markers: Array<{
    start: number;
    end: number;
    type: "placeholder" | "expression";
    content: string;
  }> = [];

  // Find all expressions first ({{ ... }})
  for (const match of value.matchAll(EXPRESSION_REGEX)) {
    if (match.index !== undefined) {
      markers.push({
        start: match.index,
        end: match.index + match[0].length,
        type: "expression",
        content: match[1].trim(),
      });
    }
  }

  // Find all placeholders (@variable or @"quoted") that don't overlap with expressions
  for (const match of value.matchAll(PLACEHOLDER_REGEX)) {
    if (match.index !== undefined) {
      const start = match.index;
      const end = match.index + match[0].length;
      // Check if this overlaps with any expression
      const overlaps = markers.some(
        (m) =>
          (start >= m.start && start < m.end) || (end > m.start && end <= m.end)
      );
      if (!overlaps) {
        // match[1] is quoted name, match[2] is unquoted identifier
        const content = match[1] ?? match[2];
        markers.push({
          start,
          end,
          type: "placeholder",
          content,
        });
      }
    }
  }

  // Sort markers by position
  markers.sort((a, b) => a.start - b.start);

  // Build tokens
  let lastIndex = 0;
  for (const marker of markers) {
    // Add text before this marker
    if (marker.start > lastIndex) {
      tokens.push({
        type: "text",
        content: value.slice(lastIndex, marker.start),
        start: lastIndex,
        end: marker.start,
      });
    }
    tokens.push({
      type: marker.type,
      content: marker.content,
      start: marker.start,
      end: marker.end,
    });
    lastIndex = marker.end;
  }

  // Add remaining text
  if (lastIndex < value.length) {
    tokens.push({
      type: "text",
      content: value.slice(lastIndex),
      start: lastIndex,
      end: value.length,
    });
  }

  return tokens;
}

/**
 * Validate an expression against available inputs
 */
function isExpressionValid(
  expression: string,
  availableInputs: AppInput[]
): boolean {
  const availableVars = availableInputs
    .map((i) => i.title)
    .filter((t): t is string => t !== null && t !== undefined);
  const result = validateExpression(expression, availableVars);
  return result.isValid;
}

/**
 * Parse a string value with @placeholder and {{ expression }} syntax into Lexical editor state.
 * Call this within an editor.update() callback.
 */
export function parseValueToEditorState(
  value: string,
  availableInputs: AppInput[]
): void {
  const root = $getRoot();
  root.clear();

  const paragraph = $createParagraphNode();
  const tokens = tokenize(value);

  for (const token of tokens) {
    if (token.type === "text") {
      paragraph.append($createTextNode(token.content));
    } else if (token.type === "placeholder") {
      const inputTitle = token.content;
      const isValid = isCurrentUserPlaceholder(inputTitle)
        ? isValidCurrentUserPlaceholder(inputTitle)
        : availableInputs.some((i) => i.title === inputTitle);
      paragraph.append($createPlaceholderNode(inputTitle, isValid));
    } else if (token.type === "expression") {
      const isValid = isExpressionValid(token.content, availableInputs);
      paragraph.append($createExpressionNode(token.content, isValid));
    }
  }

  root.append(paragraph);
}

/**
 * Serialize Lexical editor state back to a string with @placeholder and {{ expression }} syntax.
 * Call this within an editorState.read() callback.
 */
export function serializeEditorStateToString(): string {
  const root = $getRoot();
  let result = "";

  const paragraph = root.getFirstChild();
  if (!paragraph) {
    return result;
  }

  const children = paragraph.getChildren();
  for (const child of children) {
    if ($isPlaceholderNode(child)) {
      const title = child.getInputTitle();
      result += needsQuoting(title) ? `@"${title}"` : `@${title}`;
    } else if ($isExpressionNode(child)) {
      result += `{{ ${child.getExpression()} }}`;
    } else if ($isTextNode(child)) {
      result += child.getTextContent();
    }
  }

  return result;
}

import {
  $createParagraphNode,
  $createTextNode,
  $getRoot,
  $isTextNode,
} from "lexical";
import type { AppInput } from "../input-with-placeholders.types";
import { $createPlaceholderNode, $isPlaceholderNode } from "./PlaceholderNode";

/**
 * Parse a string value with {placeholder} syntax into Lexical editor state.
 * Call this within an editor.update() callback.
 */
export function parseValueToEditorState(
  value: string,
  availableInputs: AppInput[]
): void {
  const root = $getRoot();
  root.clear();

  const paragraph = $createParagraphNode();
  const regex = /\{([^{}]+)\}/g;
  let lastIndex = 0;

  for (const match of value.matchAll(regex)) {
    // Add text before placeholder
    if (match.index !== undefined && match.index > lastIndex) {
      const text = value.slice(lastIndex, match.index);
      paragraph.append($createTextNode(text));
    }

    // Add placeholder node
    const inputTitle = match[1];
    const isValid = availableInputs.some((i) => i.title === inputTitle);
    paragraph.append($createPlaceholderNode(inputTitle, isValid));

    if (match.index !== undefined) {
      lastIndex = match.index + match[0].length;
    }
  }

  // Add remaining text
  if (lastIndex < value.length) {
    paragraph.append($createTextNode(value.slice(lastIndex)));
  }

  root.append(paragraph);
}

/**
 * Serialize Lexical editor state back to a string with {placeholder} syntax.
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
      result += `{${child.getInputTitle()}}`;
    } else if ($isTextNode(child)) {
      result += child.getTextContent();
    }
  }

  return result;
}

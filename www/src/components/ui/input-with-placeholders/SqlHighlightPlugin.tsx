import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { $createTextNode, $isTextNode, TextNode } from "lexical";
import { useEffect } from "react";

import { getSqlTokenStyle, tokenizeSqlText } from "./sql-highlighter.utils";

const isTokenStyled = (node: TextNode, style: string | null): boolean =>
  (node.getStyle() || "") === (style || "");

const createStyledTextNode = (
  value: string,
  style: string | null
): TextNode => {
  const node = $createTextNode(value);
  if (style) {
    node.setStyle(style);
  }
  return node;
};

export function SqlHighlightPlugin(): null {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    return editor.registerNodeTransform(TextNode, (node) => {
      if (!$isTextNode(node)) {
        return;
      }

      const text = node.getTextContent();
      if (text.length === 0) {
        if (node.getStyle()) {
          node.setStyle("");
        }
        return;
      }

      const tokens = tokenizeSqlText(text);
      if (tokens.length === 1) {
        const token = tokens[0];
        const style = getSqlTokenStyle(token.type);
        if (!isTokenStyled(node, style)) {
          node.setStyle(style ?? "");
        }
        return;
      }

      const parent = node.getParent();
      if (!parent) {
        return;
      }

      const firstToken = tokens[0];
      const firstNode = createStyledTextNode(
        firstToken.value,
        getSqlTokenStyle(firstToken.type)
      );
      node.replace(firstNode);

      let current = firstNode;
      for (const token of tokens.slice(1)) {
        const nextNode = createStyledTextNode(
          token.value,
          getSqlTokenStyle(token.type)
        );
        current.insertAfter(nextNode);
        current = nextNode;
      }
    });
  }, [editor]);

  return null;
}

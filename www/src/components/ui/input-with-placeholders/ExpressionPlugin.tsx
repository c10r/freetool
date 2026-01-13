import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import {
  $createTextNode,
  $getNodeByKey,
  $getSelection,
  $isRangeSelection,
  $isTextNode,
  COMMAND_PRIORITY_HIGH,
  KEY_DOWN_COMMAND,
} from "lexical";
import { useEffect, useRef } from "react";
import { validateExpression } from "@/lib/expression-evaluator";
import type { AppInput } from "../input-with-placeholders.types";
import { $createExpressionNode, $isExpressionNode } from "./ExpressionNode";
import {
  type ExpressionEditorPayload,
  OPEN_EXPRESSION_EDITOR_COMMAND,
  SET_EXPRESSION_COMMAND,
} from "./expression-commands";

interface ExpressionPluginProps {
  availableInputs: AppInput[];
  onOpenExpressionEditor: (
    payload: ExpressionEditorPayload & { expression?: string }
  ) => void;
  onClosePlaceholderPopover?: () => void;
}

export function ExpressionPlugin({
  availableInputs,
  onOpenExpressionEditor,
  onClosePlaceholderPopover,
}: ExpressionPluginProps): null {
  const [editor] = useLexicalComposerContext();
  // Track consecutive "{" key presses
  const lastBraceTime = useRef<number>(0);
  const lastBracePosition = useRef<{ text: string; offset: number } | null>(
    null
  );

  useEffect(() => {
    // Listen for "{{" pattern (second "{" after first)
    const unregisterKeyDown = editor.registerCommand(
      KEY_DOWN_COMMAND,
      (event: KeyboardEvent) => {
        if (event.key === "{") {
          const now = Date.now();
          const selection = editor.getEditorState().read(() => $getSelection());

          // Check if this is a second "{" typed quickly after the first
          if (
            now - lastBraceTime.current < 500 &&
            lastBracePosition.current !== null
          ) {
            // This is "{{" - open expression editor
            event.preventDefault();

            // Close placeholder popover if it opened (due to 150ms delay race)
            onClosePlaceholderPopover?.();

            // Remove the first "{" and open expression editor
            editor.update(() => {
              const currentSelection = $getSelection();
              if (!$isRangeSelection(currentSelection)) {
                return;
              }

              const anchorNode = currentSelection.anchor.getNode();
              if ($isTextNode(anchorNode)) {
                const text = anchorNode.getTextContent();
                const lastBraceIndex = text.lastIndexOf("{");
                if (lastBraceIndex !== -1) {
                  // Remove the trailing "{"
                  anchorNode.setTextContent(text.slice(0, lastBraceIndex));
                }
              }
            });

            // Open expression editor for insert mode
            setTimeout(() => {
              onOpenExpressionEditor({
                mode: "insert",
              });
            }, 0);

            lastBraceTime.current = 0;
            lastBracePosition.current = null;
            return true;
          }

          // Track this "{" for potential "{{" detection
          lastBraceTime.current = now;
          if ($isRangeSelection(selection)) {
            const anchorNode = selection.anchor.getNode();
            if ($isTextNode(anchorNode)) {
              lastBracePosition.current = {
                text: anchorNode.getTextContent(),
                offset: selection.anchor.offset,
              };
            }
          }
        } else {
          // Any other key resets the "{" tracking
          lastBraceTime.current = 0;
          lastBracePosition.current = null;
        }

        return false;
      },
      COMMAND_PRIORITY_HIGH
    );

    // Listen for expression editor open command (from pill clicks)
    const unregisterOpenEditor = editor.registerCommand(
      OPEN_EXPRESSION_EDITOR_COMMAND,
      (payload) => {
        // Get the expression from the node if in edit mode
        let expression: string | undefined;
        if (payload.nodeKey) {
          const nodeKey = payload.nodeKey;
          editor.getEditorState().read(() => {
            const node = $getNodeByKey(nodeKey);
            if ($isExpressionNode(node)) {
              expression = node.getExpression();
            }
          });
        }
        onOpenExpressionEditor({ ...payload, expression });
        return true;
      },
      COMMAND_PRIORITY_HIGH
    );

    // Listen for expression set command (from editor dialog)
    const unregisterSetExpression = editor.registerCommand(
      SET_EXPRESSION_COMMAND,
      (payload) => {
        const { expression, nodeKey } = payload;

        // Validate expression
        const availableVars = availableInputs
          .map((i) => i.title)
          .filter((t): t is string => t !== null && t !== undefined);
        const validation = validateExpression(expression, availableVars);

        editor.update(() => {
          if (nodeKey) {
            // Edit mode - update existing expression node
            const node = $getNodeByKey(nodeKey);
            if ($isExpressionNode(node)) {
              node.setExpression(expression);
              node.setIsValid(validation.isValid);
            }
          } else {
            // Insert mode - create new expression node
            const selection = $getSelection();
            if (!$isRangeSelection(selection)) {
              return;
            }

            const anchorNode = selection.anchor.getNode();
            const expressionNode = $createExpressionNode(
              expression,
              validation.isValid
            );

            if ($isTextNode(anchorNode)) {
              const text = anchorNode.getTextContent();
              const offset = selection.anchor.offset;

              // Insert at cursor position
              if (offset === text.length) {
                // At end of text
                anchorNode.insertAfter(expressionNode);
              } else if (offset === 0) {
                // At start of text
                anchorNode.insertBefore(expressionNode);
              } else {
                // In middle - split text
                const before = text.slice(0, offset);
                const after = text.slice(offset);
                anchorNode.setTextContent(before);
                anchorNode.insertAfter(expressionNode);
                if (after) {
                  const afterNode = $createTextNode(after);
                  expressionNode.insertAfter(afterNode);
                }
              }
            } else {
              // Not in a text node - try to insert at selection
              selection.insertNodes([expressionNode]);
            }

            // Move cursor after the expression node
            expressionNode.selectNext(0, 0);
          }
        });

        return true;
      },
      COMMAND_PRIORITY_HIGH
    );

    return () => {
      unregisterKeyDown();
      unregisterOpenEditor();
      unregisterSetExpression();
    };
  }, [
    editor,
    availableInputs,
    onOpenExpressionEditor,
    onClosePlaceholderPopover,
  ]);

  return null;
}

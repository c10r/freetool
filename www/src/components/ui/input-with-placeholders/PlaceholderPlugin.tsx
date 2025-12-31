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
import { useEffect } from "react";
import type { AppInput } from "../input-with-placeholders.types";
import {
  OPEN_PLACEHOLDER_POPOVER_COMMAND,
  type PopoverPayload,
  RESTORE_CURSOR_AFTER_BRACE_COMMAND,
  SELECT_PLACEHOLDER_INPUT_COMMAND,
} from "./commands";
import { $createPlaceholderNode, $isPlaceholderNode } from "./PlaceholderNode";

interface PlaceholderPluginProps {
  availableInputs: AppInput[];
  onOpenPopover: (payload: PopoverPayload) => void;
  onClosePopover: () => void;
  isPopoverOpen: boolean;
  popoverMode: "insert" | "edit";
}

export function PlaceholderPlugin({
  availableInputs,
  onOpenPopover,
  onClosePopover,
  isPopoverOpen,
  popoverMode,
}: PlaceholderPluginProps): null {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    // Listen for "{" key and Backspace
    const unregisterKeyDown = editor.registerCommand(
      KEY_DOWN_COMMAND,
      (event: KeyboardEvent) => {
        if (event.key === "{" && availableInputs.length > 0) {
          // Let the character be inserted first, then open popover
          setTimeout(() => {
            onOpenPopover({
              mode: "insert",
            });
          }, 0);
        }

        // Close popover if backspace deletes the opening brace
        if (
          event.key === "Backspace" &&
          isPopoverOpen &&
          popoverMode === "insert"
        ) {
          // Check after the character is deleted
          setTimeout(() => {
            editor.getEditorState().read(() => {
              const selection = $getSelection();
              if (!$isRangeSelection(selection)) {
                return;
              }
              const anchorNode = selection.anchor.getNode();
              if ($isTextNode(anchorNode)) {
                const text = anchorNode.getTextContent();
                // If there's no trailing "{", close the popover
                if (!text.endsWith("{")) {
                  onClosePopover();
                }
              } else {
                // Cursor is not in a text node, close popover
                onClosePopover();
              }
            });
          }, 0);
        }

        return false; // Don't prevent default
      },
      COMMAND_PRIORITY_HIGH
    );

    // Listen for popover open command (from pill clicks)
    const unregisterOpenPopover = editor.registerCommand(
      OPEN_PLACEHOLDER_POPOVER_COMMAND,
      (payload) => {
        onOpenPopover(payload);
        return true;
      },
      COMMAND_PRIORITY_HIGH
    );

    // Listen for cursor restoration after brace
    const unregisterRestoreCursor = editor.registerCommand(
      RESTORE_CURSOR_AFTER_BRACE_COMMAND,
      () => {
        editor.update(() => {
          const selection = $getSelection();
          if (!$isRangeSelection(selection)) {
            return;
          }

          // Find the text node with trailing "{"
          const root = selection.anchor.getTopLevelElement();
          if (!root) {
            return;
          }

          const children = root.getChildren();
          for (const child of children) {
            if ($isTextNode(child)) {
              const text = child.getTextContent();
              const braceIndex = text.lastIndexOf("{");
              if (braceIndex !== -1) {
                // Position cursor right after the "{"
                child.select(braceIndex + 1, braceIndex + 1);
                return;
              }
            }
          }
        });
        return true;
      },
      COMMAND_PRIORITY_HIGH
    );

    // Listen for input selection from popover
    const unregisterSelectInput = editor.registerCommand(
      SELECT_PLACEHOLDER_INPUT_COMMAND,
      (payload) => {
        const { inputTitle, nodeKey } = payload;

        editor.update(() => {
          if (nodeKey) {
            // Edit mode - replace existing placeholder
            const node = $getNodeByKey(nodeKey);
            if ($isPlaceholderNode(node)) {
              node.setInputTitle(inputTitle);
              node.setIsValid(true);
            }
          } else {
            // Insert mode - find and remove trailing "{"
            const selection = $getSelection();
            if (!$isRangeSelection(selection)) {
              return;
            }

            const anchorNode = selection.anchor.getNode();
            if ($isTextNode(anchorNode)) {
              const text = anchorNode.getTextContent();
              const lastBraceIndex = text.lastIndexOf("{");

              if (lastBraceIndex !== -1) {
                const before = text.slice(0, lastBraceIndex);
                const after = text.slice(lastBraceIndex + 1);

                // Set text before the brace
                anchorNode.setTextContent(before);

                // Create placeholder node
                const placeholder = $createPlaceholderNode(inputTitle, true);

                if (before) {
                  // Insert placeholder after the text node
                  anchorNode.insertAfter(placeholder);
                } else {
                  // Replace empty text node
                  anchorNode.replace(placeholder);
                }

                // Add remaining text if any
                if (after) {
                  const afterText = $createTextNode(after);
                  placeholder.insertAfter(afterText);
                  afterText.select(0, 0);
                } else {
                  // Move selection after placeholder
                  placeholder.selectNext(0, 0);
                }
              }
            }
          }
        });

        return true;
      },
      COMMAND_PRIORITY_HIGH
    );

    return () => {
      unregisterKeyDown();
      unregisterOpenPopover();
      unregisterRestoreCursor();
      unregisterSelectInput();
    };
  }, [
    editor,
    availableInputs.length,
    onOpenPopover,
    onClosePopover,
    isPopoverOpen,
    popoverMode,
  ]);

  return null;
}

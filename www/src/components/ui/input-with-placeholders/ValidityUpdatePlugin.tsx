import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { $getRoot } from "lexical";
import { useEffect } from "react";
import type { AppInput } from "../input-with-placeholders.types";
import {
  isCurrentUserPlaceholder,
  isValidCurrentUserPlaceholder,
} from "./current-user";
import { $isPlaceholderNode } from "./PlaceholderNode";

interface ValidityUpdatePluginProps {
  availableInputs: AppInput[];
}

export function ValidityUpdatePlugin({
  availableInputs,
}: ValidityUpdatePluginProps): null {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    editor.update(() => {
      const root = $getRoot();
      const paragraph = root.getFirstChild();
      if (!paragraph) {
        return;
      }

      const children = paragraph.getChildren();
      for (const child of children) {
        if ($isPlaceholderNode(child)) {
          const inputTitle = child.getInputTitle();
          // Check if it's a current_user placeholder or a regular app input
          const isValid = isCurrentUserPlaceholder(inputTitle)
            ? isValidCurrentUserPlaceholder(inputTitle)
            : availableInputs.some((i) => i.title === inputTitle);
          if (child.getIsValid() !== isValid) {
            child.setIsValid(isValid);
          }
        }
      }
    });
  }, [editor, availableInputs]);

  return null;
}

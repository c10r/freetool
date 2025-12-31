import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { BLUR_COMMAND, COMMAND_PRIORITY_LOW } from "lexical";
import { useEffect } from "react";

interface OnBlurPluginProps {
  onBlur?: () => void;
}

export function OnBlurPlugin({ onBlur }: OnBlurPluginProps): null {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!onBlur) {
      return;
    }

    return editor.registerCommand(
      BLUR_COMMAND,
      () => {
        onBlur();
        return false; // Don't prevent default
      },
      COMMAND_PRIORITY_LOW
    );
  }, [editor, onBlur]);

  return null;
}

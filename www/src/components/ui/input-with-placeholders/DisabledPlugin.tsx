import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { useEffect } from "react";

interface DisabledPluginProps {
  disabled: boolean;
}

export function DisabledPlugin({ disabled }: DisabledPluginProps): null {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    editor.setEditable(!disabled);
  }, [editor, disabled]);

  return null;
}

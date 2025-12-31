import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { useEffect, useRef } from "react";
import type { AppInput } from "../input-with-placeholders.types";
import {
  parseValueToEditorState,
  serializeEditorStateToString,
} from "./serialization";

interface ValueSyncPluginProps {
  value: string;
  onChange: (value: string) => void;
  availableInputs: AppInput[];
}

export function ValueSyncPlugin({
  value,
  onChange,
  availableInputs,
}: ValueSyncPluginProps): null {
  const [editor] = useLexicalComposerContext();
  const isExternalUpdate = useRef(false);
  const lastValue = useRef(value);
  const isInitialized = useRef(false);

  // Initialize editor with value on mount
  useEffect(() => {
    if (!isInitialized.current) {
      isInitialized.current = true;
      isExternalUpdate.current = true;

      editor.update(() => {
        parseValueToEditorState(value, availableInputs);
      });

      isExternalUpdate.current = false;
      lastValue.current = value;
    }
  }, [editor, value, availableInputs]);

  // Sync external value changes to editor
  useEffect(() => {
    if (value !== lastValue.current && isInitialized.current) {
      isExternalUpdate.current = true;
      lastValue.current = value;

      editor.update(() => {
        parseValueToEditorState(value, availableInputs);
      });

      isExternalUpdate.current = false;
    }
  }, [value, editor, availableInputs]);

  // Listen for editor changes and sync to external value
  useEffect(() => {
    return editor.registerUpdateListener(({ editorState }) => {
      if (isExternalUpdate.current) {
        return;
      }

      editorState.read(() => {
        const serialized = serializeEditorStateToString();
        if (serialized !== lastValue.current) {
          lastValue.current = serialized;
          onChange(serialized);
        }
      });
    });
  }, [editor, onChange]);

  return null;
}

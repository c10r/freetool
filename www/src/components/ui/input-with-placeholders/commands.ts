import type { LexicalCommand, NodeKey } from "lexical";
import { createCommand } from "lexical";

export interface PopoverPayload {
  nodeKey?: NodeKey;
  mode: "insert" | "edit";
}

export interface SelectInputPayload {
  inputTitle: string;
  nodeKey?: NodeKey;
}

export const OPEN_PLACEHOLDER_POPOVER_COMMAND: LexicalCommand<PopoverPayload> =
  createCommand("OPEN_PLACEHOLDER_POPOVER");

export const SELECT_PLACEHOLDER_INPUT_COMMAND: LexicalCommand<SelectInputPayload> =
  createCommand("SELECT_PLACEHOLDER_INPUT");

export const RESTORE_CURSOR_AFTER_BRACE_COMMAND: LexicalCommand<void> =
  createCommand("RESTORE_CURSOR_AFTER_BRACE");

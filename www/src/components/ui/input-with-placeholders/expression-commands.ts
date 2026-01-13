import type { LexicalCommand, NodeKey } from "lexical";
import { createCommand } from "lexical";

export interface ExpressionEditorPayload {
  nodeKey?: NodeKey;
  mode: "insert" | "edit";
}

export interface SetExpressionPayload {
  expression: string;
  nodeKey?: NodeKey;
}

export const OPEN_EXPRESSION_EDITOR_COMMAND: LexicalCommand<ExpressionEditorPayload> =
  createCommand("OPEN_EXPRESSION_EDITOR");

export const SET_EXPRESSION_COMMAND: LexicalCommand<SetExpressionPayload> =
  createCommand("SET_EXPRESSION");

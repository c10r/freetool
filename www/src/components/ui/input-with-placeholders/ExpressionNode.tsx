import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import type {
  DOMConversionMap,
  DOMExportOutput,
  EditorConfig,
  LexicalEditor,
  LexicalNode,
  NodeKey,
  SerializedLexicalNode,
  Spread,
} from "lexical";
import { $applyNodeReplacement, DecoratorNode } from "lexical";
import type { JSX } from "react";
import { cn } from "@/lib/utils";
import { OPEN_EXPRESSION_EDITOR_COMMAND } from "./expression-commands";

export type SerializedExpressionNode = Spread<
  {
    expression: string;
    isValid: boolean;
  },
  SerializedLexicalNode
>;

interface ExpressionPillProps {
  nodeKey: NodeKey;
  expression: string;
  isValid: boolean;
}

function ExpressionPill({
  nodeKey,
  expression,
  isValid,
}: ExpressionPillProps): JSX.Element {
  const [editor] = useLexicalComposerContext();

  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    editor.dispatchCommand(OPEN_EXPRESSION_EDITOR_COMMAND, {
      nodeKey,
      mode: "edit",
    });
  };

  // Truncate long expressions for display
  const displayText =
    expression.length > 30 ? `${expression.slice(0, 27)}...` : expression;

  return (
    <span
      onClick={handleClick}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          handleClick(e as unknown as React.MouseEvent);
        }
      }}
      className={cn(
        "inline-flex items-center cursor-pointer select-none mx-0.5 align-baseline text-xs font-medium rounded-sm p-1",
        isValid
          ? "bg-purple-100 text-purple-800"
          : "text-destructive bg-destructive/10 line-through"
      )}
      title={expression}
    >
      {"{{"}
      <span className="mx-0.5">{displayText}</span>
      {"}}"}
    </span>
  );
}

export class ExpressionNode extends DecoratorNode<JSX.Element> {
  __expression: string;
  __isValid: boolean;

  static getType(): string {
    return "expression";
  }

  static clone(node: ExpressionNode): ExpressionNode {
    return new ExpressionNode(node.__expression, node.__isValid, node.__key);
  }

  constructor(expression: string, isValid: boolean, key?: NodeKey) {
    super(key);
    this.__expression = expression;
    this.__isValid = isValid;
  }

  static importJSON(serializedNode: SerializedExpressionNode): ExpressionNode {
    return $createExpressionNode(
      serializedNode.expression,
      serializedNode.isValid
    );
  }

  exportJSON(): SerializedExpressionNode {
    return {
      type: "expression",
      version: 1,
      expression: this.__expression,
      isValid: this.__isValid,
    };
  }

  static importDOM(): DOMConversionMap | null {
    return null;
  }

  exportDOM(_editor: LexicalEditor): DOMExportOutput {
    const element = document.createElement("span");
    element.setAttribute("data-expression", this.__expression);
    element.textContent = `{{ ${this.__expression} }}`;
    return { element };
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const span = document.createElement("span");
    span.className = this.__isValid
      ? "lexical-expression-pill lexical-expression-valid"
      : "lexical-expression-pill lexical-expression-invalid";
    return span;
  }

  updateDOM(): boolean {
    // Return false to indicate DOM doesn't need to be recreated
    return false;
  }

  decorate(): JSX.Element {
    return (
      <ExpressionPill
        nodeKey={this.__key}
        expression={this.__expression}
        isValid={this.__isValid}
      />
    );
  }

  isInline(): boolean {
    return true;
  }

  isIsolated(): boolean {
    return false;
  }

  getExpression(): string {
    return this.__expression;
  }

  setExpression(expression: string): this {
    const writable = this.getWritable();
    writable.__expression = expression;
    return writable;
  }

  getIsValid(): boolean {
    return this.__isValid;
  }

  setIsValid(isValid: boolean): this {
    const writable = this.getWritable();
    writable.__isValid = isValid;
    return writable;
  }
}

export function $createExpressionNode(
  expression: string,
  isValid: boolean
): ExpressionNode {
  return $applyNodeReplacement(new ExpressionNode(expression, isValid));
}

export function $isExpressionNode(
  node: LexicalNode | null | undefined
): node is ExpressionNode {
  return node instanceof ExpressionNode;
}

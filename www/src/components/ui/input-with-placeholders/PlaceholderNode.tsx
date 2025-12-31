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
import { OPEN_PLACEHOLDER_POPOVER_COMMAND } from "./commands";

export type SerializedPlaceholderNode = Spread<
  {
    inputTitle: string;
    isValid: boolean;
  },
  SerializedLexicalNode
>;

interface PlaceholderPillProps {
  nodeKey: NodeKey;
  inputTitle: string;
  isValid: boolean;
}

function PlaceholderPill({
  nodeKey,
  inputTitle,
  isValid,
}: PlaceholderPillProps): JSX.Element {
  const [editor] = useLexicalComposerContext();

  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    editor.dispatchCommand(OPEN_PLACEHOLDER_POPOVER_COMMAND, {
      nodeKey,
      mode: "edit",
    });
  };

  return (
    <span
      onClick={handleClick}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          handleClick(e as unknown as React.MouseEvent);
        }
      }}
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        "cursor-pointer select-none mx-0.5 align-baseline",
        isValid
          ? "bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20"
          : "bg-destructive/10 text-destructive border border-destructive/20 hover:bg-destructive/20 line-through"
      )}
    >
      {inputTitle}
    </span>
  );
}

export class PlaceholderNode extends DecoratorNode<JSX.Element> {
  __inputTitle: string;
  __isValid: boolean;

  static getType(): string {
    return "placeholder";
  }

  static clone(node: PlaceholderNode): PlaceholderNode {
    return new PlaceholderNode(node.__inputTitle, node.__isValid, node.__key);
  }

  constructor(inputTitle: string, isValid: boolean, key?: NodeKey) {
    super(key);
    this.__inputTitle = inputTitle;
    this.__isValid = isValid;
  }

  static importJSON(
    serializedNode: SerializedPlaceholderNode
  ): PlaceholderNode {
    return $createPlaceholderNode(
      serializedNode.inputTitle,
      serializedNode.isValid
    );
  }

  exportJSON(): SerializedPlaceholderNode {
    return {
      type: "placeholder",
      version: 1,
      inputTitle: this.__inputTitle,
      isValid: this.__isValid,
    };
  }

  static importDOM(): DOMConversionMap | null {
    return null;
  }

  exportDOM(_editor: LexicalEditor): DOMExportOutput {
    const element = document.createElement("span");
    element.setAttribute("data-placeholder-title", this.__inputTitle);
    element.textContent = this.__inputTitle;
    return { element };
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const span = document.createElement("span");
    span.className = this.__isValid
      ? "lexical-placeholder-pill lexical-placeholder-valid"
      : "lexical-placeholder-pill lexical-placeholder-invalid";
    return span;
  }

  updateDOM(): boolean {
    // Return false to indicate DOM doesn't need to be recreated
    return false;
  }

  decorate(): JSX.Element {
    return (
      <PlaceholderPill
        nodeKey={this.__key}
        inputTitle={this.__inputTitle}
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

  getInputTitle(): string {
    return this.__inputTitle;
  }

  setInputTitle(inputTitle: string): this {
    const writable = this.getWritable();
    writable.__inputTitle = inputTitle;
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

export function $createPlaceholderNode(
  inputTitle: string,
  isValid: boolean
): PlaceholderNode {
  return $applyNodeReplacement(new PlaceholderNode(inputTitle, isValid));
}

export function $isPlaceholderNode(
  node: LexicalNode | null | undefined
): node is PlaceholderNode {
  return node instanceof PlaceholderNode;
}

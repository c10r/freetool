import { LexicalComposer } from "@lexical/react/LexicalComposer";
import { ContentEditable } from "@lexical/react/LexicalContentEditable";
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary";
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin";
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin";
import type { NodeKey } from "lexical";
import { useCallback, useMemo, useRef, useState } from "react";

import { cn } from "@/lib/utils";
import { Badge } from "../badge";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "../command";
import type { InputWithPlaceholdersProps } from "../input-with-placeholders.types";
import { Popover, PopoverAnchor, PopoverContent } from "../popover";
import {
  type PopoverPayload,
  RESTORE_CURSOR_AFTER_BRACE_COMMAND,
  SELECT_PLACEHOLDER_INPUT_COMMAND,
} from "./commands";
import { CURRENT_USER_PREFIX, CURRENT_USER_PROPERTIES } from "./current-user";
import { DisabledPlugin } from "./DisabledPlugin";
import { ExpressionEditor } from "./ExpressionEditor";
import { ExpressionNode } from "./ExpressionNode";
import { ExpressionPlugin } from "./ExpressionPlugin";
import {
  type ExpressionEditorPayload,
  SET_EXPRESSION_COMMAND,
} from "./expression-commands";
import { OnBlurPlugin } from "./OnBlurPlugin";
import { PlaceholderNode } from "./PlaceholderNode";
import { PlaceholderPlugin } from "./PlaceholderPlugin";
import { SqlHighlightPlugin } from "./SqlHighlightPlugin";
import { theme } from "./theme";
import { ValidityUpdatePlugin } from "./ValidityUpdatePlugin";
import { ValueSyncPlugin } from "./ValueSyncPlugin";

interface PopoverState {
  isOpen: boolean;
  triggerType: "insert" | "edit";
  editingNodeKey?: NodeKey;
}

interface ExpressionEditorState {
  isOpen: boolean;
  mode: "insert" | "edit";
  editingNodeKey?: NodeKey;
  initialExpression?: string;
}

export function InputWithPlaceholders({
  value,
  onChange,
  onBlur,
  availableInputs,
  placeholder,
  disabled = false,
  className,
  inputClassName,
  id,
  "aria-label": ariaLabel,
  disableExpressions = false,
  syntaxHighlighting,
}: InputWithPlaceholdersProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const editorRef = useRef<{
    dispatchCommand: (cmd: unknown, payload: unknown) => void;
    restoreCursorAfterBrace: () => void;
  } | null>(null);
  const [popoverState, setPopoverState] = useState<PopoverState>({
    isOpen: false,
    triggerType: "insert",
  });
  const [expressionEditorState, setExpressionEditorState] =
    useState<ExpressionEditorState>({
      isOpen: false,
      mode: "insert",
    });
  const [searchFilter, setSearchFilter] = useState("");

  const initialConfig = useMemo(
    () => ({
      namespace: "InputWithPlaceholders",
      theme,
      nodes: [PlaceholderNode, ExpressionNode],
      onError: (_error: Error) => {
        // Errors are handled by LexicalErrorBoundary
      },
      editable: !disabled,
    }),
    [disabled]
  );

  const handleOpenPopover = useCallback((payload: PopoverPayload) => {
    setPopoverState({
      isOpen: true,
      triggerType: payload.mode,
      editingNodeKey: payload.nodeKey,
    });
    setSearchFilter("");
  }, []);

  const handleSelectInput = useCallback(
    (inputTitle: string) => {
      if (editorRef.current) {
        editorRef.current.dispatchCommand(SELECT_PLACEHOLDER_INPUT_COMMAND, {
          inputTitle,
          nodeKey: popoverState.editingNodeKey,
        });
      }
      setPopoverState({ isOpen: false, triggerType: "insert" });
    },
    [popoverState.editingNodeKey]
  );

  const handlePopoverOpenChange = useCallback(
    (open: boolean) => {
      if (!open) {
        const wasInsertMode = popoverState.triggerType === "insert";
        setPopoverState((prev) => ({ ...prev, isOpen: false }));
        // Return focus to the editor when popover closes
        requestAnimationFrame(() => {
          const editorElement = containerRef.current?.querySelector(
            '[contenteditable="true"]'
          );
          if (editorElement instanceof HTMLElement) {
            editorElement.focus();
            // Restore cursor to after the "@" if we were in insert mode
            if (wasInsertMode && editorRef.current) {
              editorRef.current.restoreCursorAfterBrace();
            }
          }
        });
      }
    },
    [popoverState.triggerType]
  );

  const handleClosePopover = useCallback(() => {
    setPopoverState((prev) => ({ ...prev, isOpen: false }));
  }, []);

  // Expression editor handlers
  const handleOpenExpressionEditor = useCallback(
    (payload: ExpressionEditorPayload & { expression?: string }) => {
      setExpressionEditorState({
        isOpen: true,
        mode: payload.mode,
        editingNodeKey: payload.nodeKey,
        initialExpression: payload.expression || "",
      });
    },
    []
  );

  const handleExpressionEditorOpenChange = useCallback((open: boolean) => {
    if (!open) {
      setExpressionEditorState((prev) => ({ ...prev, isOpen: false }));
      // Return focus to editor
      requestAnimationFrame(() => {
        const editorElement = containerRef.current?.querySelector(
          '[contenteditable="true"]'
        );
        if (editorElement instanceof HTMLElement) {
          editorElement.focus();
        }
      });
    }
  }, []);

  const handleSaveExpression = useCallback(
    (expression: string) => {
      if (editorRef.current) {
        editorRef.current.dispatchCommand(SET_EXPRESSION_COMMAND, {
          expression,
          nodeKey: expressionEditorState.editingNodeKey,
        });
      }
    },
    [expressionEditorState.editingNodeKey]
  );

  // If the user types any uppercase letter, they're being intentional about case
  const isCaseSensitive = searchFilter !== searchFilter.toLowerCase();

  // Filter current_user properties based on search
  const filteredCurrentUserProps = CURRENT_USER_PROPERTIES.filter((prop) => {
    const fullTitle = `${CURRENT_USER_PREFIX}.${prop.key}`;
    if (isCaseSensitive) {
      return (
        fullTitle.includes(searchFilter) ||
        prop.displayName.includes(searchFilter)
      );
    }
    return (
      fullTitle.toLowerCase().includes(searchFilter.toLowerCase()) ||
      prop.displayName.toLowerCase().includes(searchFilter.toLowerCase())
    );
  });

  // Filter regular app inputs based on search, excluding current_user inputs
  // (current_user inputs are shown in "User Context" section, not "App Fields")
  const filteredInputs = availableInputs.filter((input) => {
    if (!input.title || input.title.startsWith(`${CURRENT_USER_PREFIX}.`)) {
      return false;
    }
    if (isCaseSensitive) {
      return input.title.includes(searchFilter);
    }
    return input.title.toLowerCase().includes(searchFilter.toLowerCase());
  });

  return (
    <div className={cn("relative", className)} ref={containerRef}>
      <LexicalComposer initialConfig={initialConfig}>
        <EditorRefPlugin editorRef={editorRef} />
        <Popover
          open={popoverState.isOpen}
          onOpenChange={handlePopoverOpenChange}
        >
          <PopoverAnchor asChild>
            <div className="relative">
              <RichTextPlugin
                contentEditable={
                  <ContentEditable
                    id={id}
                    aria-label={ariaLabel}
                    className={cn(
                      "block min-h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-base",
                      "ring-offset-background",
                      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
                      "disabled:cursor-not-allowed disabled:opacity-50 md:text-sm",
                      "whitespace-pre-wrap break-words",
                      disabled && "cursor-not-allowed opacity-50",
                      !value && placeholder && "text-muted-foreground",
                      inputClassName
                    )}
                  />
                }
                placeholder={
                  !value && placeholder ? (
                    <div className="absolute left-3 top-2 text-muted-foreground pointer-events-none text-sm">
                      {placeholder}
                    </div>
                  ) : null
                }
                ErrorBoundary={LexicalErrorBoundary}
              />
            </div>
          </PopoverAnchor>

          <PopoverContent
            className="w-[220px] p-0"
            align="start"
            sideOffset={5}
          >
            <Command>
              <CommandInput
                placeholder="Search inputs..."
                value={searchFilter}
                onValueChange={setSearchFilter}
              />
              <CommandList>
                <CommandEmpty>No inputs found.</CommandEmpty>
                {filteredCurrentUserProps.length > 0 && (
                  <CommandGroup heading="User Context">
                    {filteredCurrentUserProps.map((prop) => {
                      const fullTitle = `${CURRENT_USER_PREFIX}.${prop.key}`;
                      return (
                        <CommandItem
                          key={fullTitle}
                          value={fullTitle}
                          onSelect={() => handleSelectInput(fullTitle)}
                        >
                          <span>{fullTitle}</span>
                          <span className="ml-auto text-xs text-muted-foreground">
                            {prop.type}
                          </span>
                        </CommandItem>
                      );
                    })}
                  </CommandGroup>
                )}
                {filteredInputs.length > 0 && (
                  <CommandGroup heading="App Fields">
                    {filteredInputs.map((input) => (
                      <CommandItem
                        key={input.title}
                        value={input.title || ""}
                        onSelect={() => {
                          if (input.title) {
                            handleSelectInput(input.title);
                          }
                        }}
                      >
                        <span>{input.title}</span>
                        {input.required && (
                          <Badge variant="outline" className="ml-auto text-xs">
                            Required
                          </Badge>
                        )}
                      </CommandItem>
                    ))}
                  </CommandGroup>
                )}
              </CommandList>
            </Command>
          </PopoverContent>
        </Popover>

        {/* Plugins */}
        <HistoryPlugin />
        <ValueSyncPlugin
          value={value}
          onChange={onChange}
          availableInputs={availableInputs}
        />
        <PlaceholderPlugin
          availableInputs={availableInputs}
          onOpenPopover={handleOpenPopover}
          onClosePopover={handleClosePopover}
          isPopoverOpen={popoverState.isOpen}
          popoverMode={popoverState.triggerType}
        />
        {syntaxHighlighting === "sql" && <SqlHighlightPlugin />}
        {!disableExpressions && (
          <ExpressionPlugin
            availableInputs={availableInputs}
            onOpenExpressionEditor={handleOpenExpressionEditor}
            onClosePlaceholderPopover={handleClosePopover}
          />
        )}
        <OnBlurPlugin onBlur={onBlur} />
        <DisabledPlugin disabled={disabled} />
        <ValidityUpdatePlugin availableInputs={availableInputs} />
      </LexicalComposer>

      {/* Expression Editor Dialog */}
      {!disableExpressions && (
        <ExpressionEditor
          open={expressionEditorState.isOpen}
          onOpenChange={handleExpressionEditorOpenChange}
          initialExpression={expressionEditorState.initialExpression}
          availableInputs={availableInputs}
          onSave={handleSaveExpression}
          mode={expressionEditorState.mode}
        />
      )}
    </div>
  );
}

// Plugin to capture editor reference for dispatching commands
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import type { MutableRefObject } from "react";

interface EditorRefPluginProps {
  editorRef: MutableRefObject<{
    dispatchCommand: (cmd: unknown, payload: unknown) => void;
    restoreCursorAfterBrace: () => void;
  } | null>;
}

function EditorRefPlugin({ editorRef }: EditorRefPluginProps): null {
  const [editor] = useLexicalComposerContext();

  // Store reference to editor for command dispatch
  editorRef.current = {
    dispatchCommand: (cmd, payload) => {
      editor.dispatchCommand(
        cmd as Parameters<typeof editor.dispatchCommand>[0],
        payload
      );
    },
    restoreCursorAfterBrace: () => {
      editor.dispatchCommand(RESTORE_CURSOR_AFTER_BRACE_COMMAND, undefined);
    },
  };

  return null;
}

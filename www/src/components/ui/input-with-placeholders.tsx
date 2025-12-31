import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { Badge } from "./badge";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "./command";
import type {
  AppInput,
  InputWithPlaceholdersProps,
  PopoverState,
  Segment,
} from "./input-with-placeholders.types";
import { Popover, PopoverAnchor, PopoverContent } from "./popover";

// Parse string value into segments (text and placeholders)
function parseValue(value: string, availableInputs: AppInput[]): Segment[] {
  const regex = /\{([^{}]+)\}/g;
  const segments: Segment[] = [];
  let lastIndex = 0;

  for (const match of value.matchAll(regex)) {
    // Add text before the placeholder
    if (match.index !== undefined && match.index > lastIndex) {
      segments.push({
        type: "text",
        content: value.slice(lastIndex, match.index),
      });
    }

    // Add the placeholder
    const inputTitle = match[1];
    const isValid = availableInputs.some((i) => i.title === inputTitle);
    segments.push({
      type: "placeholder",
      inputTitle,
      isValid,
    });

    if (match.index !== undefined) {
      lastIndex = match.index + match[0].length;
    }
  }

  // Add remaining text
  if (lastIndex < value.length) {
    segments.push({
      type: "text",
      content: value.slice(lastIndex),
    });
  }

  return segments;
}

// Convert segments back to string value
function serializeSegments(segments: Segment[]): string {
  return segments
    .map((seg) => {
      if (seg.type === "text") {
        return seg.content;
      }
      return `{${seg.inputTitle}}`;
    })
    .join("");
}

export function InputWithPlaceholders({
  value,
  onChange,
  onBlur,
  availableInputs,
  placeholder,
  disabled = false,
  className,
  id,
  "aria-label": ariaLabel,
}: InputWithPlaceholdersProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [popoverState, setPopoverState] = useState<PopoverState>({
    isOpen: false,
    triggerType: "insert",
  });
  const [searchFilter, setSearchFilter] = useState("");

  // Parse value into segments
  const segments = useMemo(
    () => parseValue(value, availableInputs),
    [value, availableInputs]
  );

  // Get text content from contenteditable, preserving placeholder markers
  const getValueFromDOM = useCallback(() => {
    if (!containerRef.current) {
      return "";
    }

    let result = "";
    const walk = (node: Node) => {
      if (node.nodeType === Node.TEXT_NODE) {
        result += node.textContent || "";
      } else if (node.nodeType === Node.ELEMENT_NODE) {
        const el = node as HTMLElement;
        // Check if this is a placeholder pill
        const placeholderTitle = el.getAttribute("data-placeholder-title");
        if (placeholderTitle) {
          result += `{${placeholderTitle}}`;
        } else {
          // Recurse into children
          for (const child of el.childNodes) {
            walk(child);
          }
        }
      }
    };

    for (const child of containerRef.current.childNodes) {
      walk(child);
    }

    return result;
  }, []);

  // Handle input changes
  const handleInput = useCallback(() => {
    const newValue = getValueFromDOM();
    if (newValue !== value) {
      onChange(newValue);
    }
  }, [getValueFromDOM, onChange, value]);

  // Handle keydown events
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === "{" && availableInputs.length > 0) {
        // Open popover after the character is inserted
        setTimeout(() => {
          setPopoverState({
            isOpen: true,
            triggerType: "insert",
          });
          setSearchFilter("");
        }, 0);
      }

      if (e.key === "Escape" && popoverState.isOpen) {
        e.preventDefault();
        setPopoverState((prev) => ({ ...prev, isOpen: false }));
      }
    },
    [availableInputs.length, popoverState.isOpen]
  );

  // Handle pill click
  const handlePillClick = useCallback((segmentIndex: number) => {
    setPopoverState({
      isOpen: true,
      triggerType: "edit",
      editingSegmentIndex: segmentIndex,
    });
    setSearchFilter("");
  }, []);

  // Handle input selection from popover
  const handleSelectInput = useCallback(
    (inputTitle: string) => {
      if (
        popoverState.triggerType === "edit" &&
        popoverState.editingSegmentIndex !== undefined
      ) {
        // Replace existing placeholder
        const newSegments = segments.map((seg, i) => {
          if (
            i === popoverState.editingSegmentIndex &&
            seg.type === "placeholder"
          ) {
            return {
              ...seg,
              inputTitle,
              isValid: true,
            };
          }
          return seg;
        });
        onChange(serializeSegments(newSegments));
      } else {
        // Insert new placeholder - remove the trailing { that triggered the popover
        const currentValue = getValueFromDOM();
        // Find the last { and replace it with the placeholder
        const lastBraceIndex = currentValue.lastIndexOf("{");
        if (lastBraceIndex !== -1) {
          const newValue =
            currentValue.slice(0, lastBraceIndex) +
            `{${inputTitle}}` +
            currentValue.slice(lastBraceIndex + 1);
          onChange(newValue);
        } else {
          // Fallback: just append
          onChange(`${currentValue}{${inputTitle}}`);
        }
      }

      setPopoverState({ isOpen: false, triggerType: "insert" });

      // Return focus to the input
      requestAnimationFrame(() => {
        containerRef.current?.focus();
      });
    },
    [popoverState, segments, onChange, getValueFromDOM]
  );

  // Close popover when clicking outside
  const handlePopoverOpenChange = useCallback((open: boolean) => {
    if (!open) {
      setPopoverState((prev) => ({ ...prev, isOpen: false }));
    }
  }, []);

  // Sync DOM with segments when value changes externally
  useEffect(() => {
    if (!containerRef.current) {
      return;
    }

    // Check if DOM content matches expected value
    const domValue = getValueFromDOM();
    if (domValue === value) {
      return;
    }

    // DOM is out of sync, need to rebuild
    // Save cursor position if possible
    const selection = window.getSelection();
    const hadFocus = document.activeElement === containerRef.current;

    // Clear and rebuild content
    containerRef.current.innerHTML = "";

    if (segments.length === 0 && !placeholder) {
      return;
    }

    for (const segment of segments) {
      if (segment.type === "text") {
        const textNode = document.createTextNode(segment.content);
        containerRef.current.appendChild(textNode);
      } else {
        // Create pill element
        const pill = document.createElement("span");
        pill.contentEditable = "false";
        pill.setAttribute("data-placeholder-title", segment.inputTitle);
        pill.className = cn(
          "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
          "cursor-pointer select-none mx-0.5 align-baseline",
          segment.isValid
            ? "bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20"
            : "bg-destructive/10 text-destructive border border-destructive/20 hover:bg-destructive/20 line-through"
        );
        pill.textContent = segment.inputTitle;
        containerRef.current.appendChild(pill);
      }
    }

    // Restore focus if needed
    if (hadFocus && selection) {
      containerRef.current.focus();
      // Move cursor to end
      const range = document.createRange();
      range.selectNodeContents(containerRef.current);
      range.collapse(false);
      selection.removeAllRanges();
      selection.addRange(range);
    }
  }, [segments, value, placeholder, getValueFromDOM]);

  // Handle click on pills within the container
  const handleContainerClick = useCallback(
    (e: React.MouseEvent) => {
      const target = e.target as HTMLElement;
      const placeholderTitle = target.getAttribute("data-placeholder-title");
      if (placeholderTitle) {
        // Find the segment index for this placeholder
        const segmentIndex = segments.findIndex(
          (seg) =>
            seg.type === "placeholder" && seg.inputTitle === placeholderTitle
        );
        if (segmentIndex !== -1) {
          handlePillClick(segmentIndex);
        }
      }
    },
    [segments, handlePillClick]
  );

  const filteredInputs = availableInputs.filter((input) =>
    input.title?.toLowerCase().includes(searchFilter.toLowerCase())
  );

  return (
    <div className="relative">
      <Popover
        open={popoverState.isOpen}
        onOpenChange={handlePopoverOpenChange}
      >
        <PopoverAnchor asChild>
          <div
            ref={containerRef}
            id={id}
            contentEditable={!disabled}
            suppressContentEditableWarning
            className={cn(
              "flex min-h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-base",
              "ring-offset-background",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
              "disabled:cursor-not-allowed disabled:opacity-50 md:text-sm",
              "whitespace-pre-wrap break-words",
              disabled && "cursor-not-allowed opacity-50",
              !value && placeholder && "text-muted-foreground",
              className
            )}
            onInput={handleInput}
            onKeyDown={handleKeyDown}
            onBlur={onBlur}
            onClick={handleContainerClick}
            aria-label={ariaLabel}
            role="textbox"
            data-placeholder={value ? undefined : placeholder}
          />
        </PopoverAnchor>
        {!value && placeholder && (
          <div className="absolute left-3 top-2 text-muted-foreground pointer-events-none text-sm">
            {placeholder}
          </div>
        )}
        <PopoverContent
          className="w-[220px] p-0"
          align="start"
          sideOffset={5}
          onOpenAutoFocus={(e) => e.preventDefault()}
        >
          <Command>
            <CommandInput
              placeholder="Search inputs..."
              value={searchFilter}
              onValueChange={setSearchFilter}
            />
            <CommandList>
              <CommandEmpty>No inputs found.</CommandEmpty>
              <CommandGroup heading="Available Inputs">
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
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
    </div>
  );
}

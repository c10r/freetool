import { Plus, Trash2 } from "lucide-react";
import { useRef } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { RadioOption } from "../types";

interface RadioOptionsEditorProps {
  options: RadioOption[];
  onChange: (options: RadioOption[]) => void;
  disabled?: boolean;
}

// Internal type that includes a unique ID for React keys
interface OptionWithId extends RadioOption {
  _id: string;
}

/**
 * Editor component for configuring radio button options.
 * Each option has a value (stored) and an optional label (displayed).
 */
export default function RadioOptionsEditor({
  options,
  onChange,
  disabled = false,
}: RadioOptionsEditorProps) {
  // Track option IDs for stable React keys
  const optionIdsRef = useRef<Map<number, string>>(new Map());
  const nextIdRef = useRef(0);

  // Generate stable IDs for options
  const getOptionId = (index: number): string => {
    const existingId = optionIdsRef.current.get(index);
    if (existingId) {
      return existingId;
    }
    const newId = `opt-${nextIdRef.current++}`;
    optionIdsRef.current.set(index, newId);
    return newId;
  };

  // Ensure we have IDs for all current options and clean up extras
  const optionsWithIds: OptionWithId[] = options.map((opt, index) => ({
    ...opt,
    _id: getOptionId(index),
  }));

  // Clean up IDs for indices that no longer exist
  const currentSize = options.length;
  for (const key of optionIdsRef.current.keys()) {
    if (key >= currentSize) {
      optionIdsRef.current.delete(key);
    }
  }

  const addOption = () => {
    onChange([...options, { value: "" }]);
  };

  const updateOption = (index: number, updates: Partial<RadioOption>) => {
    const newOptions = options.map((opt, i) =>
      i === index ? { ...opt, ...updates } : opt
    );
    onChange(newOptions);
  };

  const deleteOption = (index: number) => {
    // Shift IDs for items after the deleted one
    const newIds = new Map<number, string>();
    for (const [key, value] of optionIdsRef.current) {
      if (key < index) {
        newIds.set(key, value);
      } else if (key > index) {
        newIds.set(key - 1, value);
      }
      // Skip the deleted index
    }
    optionIdsRef.current = newIds;

    onChange(options.filter((_, i) => i !== index));
  };

  return (
    <div className="space-y-2 pl-4 border-l-2 border-muted">
      <Label className="text-xs text-muted-foreground">Radio Options</Label>
      {optionsWithIds.map((opt, index) => (
        <div key={opt._id} className="flex items-center gap-2">
          <Input
            value={opt.value}
            onChange={(e) => updateOption(index, { value: e.target.value })}
            placeholder="Value (stored)"
            className="flex-1"
            disabled={disabled}
          />
          <Input
            value={opt.label || ""}
            onChange={(e) =>
              updateOption(index, {
                label: e.target.value || undefined,
              })
            }
            placeholder="Label (optional)"
            className="flex-1"
            disabled={disabled}
          />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={() => deleteOption(index)}
            disabled={disabled || options.length <= 2}
            aria-label="Delete option"
          >
            <Trash2 size={14} />
          </Button>
        </div>
      ))}
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={addOption}
        disabled={disabled || options.length >= 50}
      >
        <Plus className="mr-1 h-3 w-3" /> Add Option
      </Button>
      {options.length < 2 && (
        <p className="text-xs text-destructive">At least 2 options required</p>
      )}
    </div>
  );
}

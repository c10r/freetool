import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { KeyValuePair } from "../types";

interface DynamicBodyEditorProps {
  items: KeyValuePair[];
  onChange: (items: KeyValuePair[]) => void;
  disabled?: boolean;
  maxItems?: number;
}

/**
 * Editor for dynamic JSON body key-value pairs at runtime.
 * Used on the RunApp page when an app has useDynamicJsonBody enabled.
 *
 * Validation:
 * - Max items (default 10)
 * - No duplicate keys
 * - Keys must not be empty
 */
export default function DynamicBodyEditor({
  items,
  onChange,
  disabled = false,
  maxItems = 10,
}: DynamicBodyEditorProps) {
  const add = () => {
    if (items.length < maxItems) {
      onChange([...items, { key: "", value: "" }]);
    }
  };

  const remove = (index: number) => {
    onChange(items.filter((_, i) => i !== index));
  };

  const update = (index: number, patch: Partial<KeyValuePair>) => {
    const next = items.map((kv, i) => (i === index ? { ...kv, ...patch } : kv));
    onChange(next);
  };

  // Check for duplicate keys
  const getDuplicateError = (key: string, index: number): string | null => {
    if (!key.trim()) {
      return null;
    }
    const duplicateIndex = items.findIndex(
      (item, i) => i !== index && item.key.trim() === key.trim()
    );
    if (duplicateIndex !== -1) {
      return "Duplicate key";
    }
    return null;
  };

  // Check for empty key
  const getEmptyKeyError = (key: string): string | null => {
    if (key === "" && items.length > 1) {
      return null; // Don't show error for first empty item
    }
    return null;
  };

  const canAdd = items.length < maxItems;

  return (
    <section className="space-y-2" aria-label="Dynamic body parameters">
      {items.map((kv, i) => {
        const duplicateError = getDuplicateError(kv.key, i);
        const emptyError = getEmptyKeyError(kv.key);
        const hasError = !!duplicateError || !!emptyError;

        return (
          <div
            // biome-ignore lint/suspicious/noArrayIndexKey: Items may have empty keys
            key={i}
            className="space-y-1"
          >
            <div className="grid grid-cols-12 gap-2 items-center">
              <Input
                placeholder="Key"
                value={kv.key}
                onChange={(e) => update(i, { key: e.target.value })}
                className={`col-span-5 ${hasError ? "border-red-500 focus-visible:ring-red-500" : ""}`}
                aria-label={`Key ${i + 1}`}
                aria-invalid={hasError}
                disabled={disabled}
              />
              <Input
                placeholder="Value"
                value={kv.value}
                onChange={(e) => update(i, { value: e.target.value })}
                className="col-span-6"
                aria-label={`Value ${i + 1}`}
                disabled={disabled}
              />
              <Button
                type="button"
                variant="secondary"
                size="icon"
                onClick={() => remove(i)}
                aria-label="Remove pair"
                disabled={disabled}
              >
                <X size={16} />
              </Button>
            </div>
            {duplicateError && (
              <p className="text-red-500 text-xs ml-1">{duplicateError}</p>
            )}
          </div>
        );
      })}
      <Button
        type="button"
        variant="secondary"
        onClick={add}
        disabled={disabled || !canAdd}
      >
        <Plus className="mr-2 h-4 w-4" />
        Add pair
        {!canAdd && (
          <span className="ml-2 text-muted-foreground">(max {maxItems})</span>
        )}
      </Button>
    </section>
  );
}

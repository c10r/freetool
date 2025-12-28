import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { KeyValuePair } from "../types";

interface KeyValueListProps {
  items: KeyValuePair[] | undefined;
  onChange?: (items: KeyValuePair[]) => void;
  onBlur?: (items: KeyValuePair[]) => void;
  ariaLabel?: string;
  readOnly?: boolean;
  readOnlyLabel?: string;
  disabled?: boolean;
}

export default function KeyValueList({
  items = [],
  onChange,
  onBlur,
  ariaLabel,
  readOnly = false,
  readOnlyLabel,
  disabled = false,
}: KeyValueListProps) {
  const add = () => onChange?.([...(items || []), { key: "", value: "" }]);
  const remove = (i: number) => onChange?.(items.filter((_, idx) => idx !== i));
  const update = (i: number, patch: Partial<KeyValuePair>) => {
    const next = items.map((kv, idx) => (idx === i ? { ...kv, ...patch } : kv));
    onChange?.(next);
  };
  if (readOnly) {
    return (
      <section className="space-y-2" aria-label={ariaLabel}>
        {readOnlyLabel && (
          <div className="text-xs text-muted-foreground">{readOnlyLabel}</div>
        )}
        {(items || []).length === 0 ? (
          <div className="text-sm text-muted-foreground italic">
            None defined
          </div>
        ) : (
          (items || []).map((kv, i) => (
            <div
              key={`${kv.key}-${i}`}
              className="grid grid-cols-12 gap-2 items-center"
            >
              <Input
                value={kv.key}
                className="col-span-5 bg-muted"
                aria-label={`Key ${i + 1}`}
                readOnly
              />
              <Input
                value={kv.value}
                className="col-span-6 bg-muted"
                aria-label={`Value ${i + 1}`}
                readOnly
              />
              <div className="col-span-1" />
            </div>
          ))
        )}
      </section>
    );
  }

  return (
    <section className="space-y-2" aria-label={ariaLabel}>
      {(items || []).map((kv, i) => (
        <fieldset
          key={`kv-${kv.key}-${i}`}
          className="grid grid-cols-12 gap-2 items-center border-0 p-0 m-0 min-w-0"
          onBlur={(e) => {
            // Only trigger onBlur if focus is leaving this entire row
            // relatedTarget can be null when clicking outside the document
            const relatedTarget = e.relatedTarget;

            if (
              !(
                relatedTarget && e.currentTarget.contains(relatedTarget as Node)
              )
            ) {
              onBlur?.(items || []);
            }
          }}
        >
          <Input
            placeholder="Key"
            value={kv.key}
            onChange={(e) => update(i, { key: e.target.value })}
            className="col-span-5"
            aria-label={`Key ${i + 1}`}
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
        </fieldset>
      ))}
      <Button
        type="button"
        variant="secondary"
        onClick={add}
        disabled={disabled}
      >
        <Plus className="mr-2 h-4 w-4" /> Add pair
      </Button>
    </section>
  );
}

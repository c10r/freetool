import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { InputWithPlaceholders } from "@/components/ui/input-with-placeholders";
import type { AppInput } from "@/components/ui/input-with-placeholders.types";
import type { KeyValuePair } from "../types";

interface KeyValueListProps {
  items: KeyValuePair[] | undefined;
  onChange?: (items: KeyValuePair[]) => void;
  onBlur?: (items: KeyValuePair[]) => void;
  ariaLabel?: string;
  readOnly?: boolean;
  readOnlyLabel?: string;
  disabled?: boolean;
  availableInputs?: AppInput[];
  blockedKeys?: string[];
}

export default function KeyValueList({
  items = [],
  onChange,
  onBlur,
  ariaLabel,
  readOnly = false,
  readOnlyLabel,
  disabled = false,
  availableInputs = [],
  blockedKeys = [],
}: KeyValueListProps) {
  const add = () => onChange?.([...(items || []), { key: "", value: "" }]);
  const remove = (i: number) => onChange?.(items.filter((_, idx) => idx !== i));

  const isKeyBlocked = (key: string): boolean => {
    const normalizedKey = key.toLowerCase();
    return blockedKeys.some((bk) => bk.toLowerCase() === normalizedKey);
  };

  const getBlockedKeyMessage = (key: string): string | null => {
    if (isKeyBlocked(key)) {
      return `"${key}" is configured in the Authorization section`;
    }
    return null;
  };

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
              // biome-ignore lint/suspicious/noArrayIndexKey: Items never reordered. Using kv.key causes focus loss.
              key={`${i}-item`}
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
      {(items || []).map((kv, i) => {
        const blockedMessage = getBlockedKeyMessage(kv.key);
        return (
          <div
            // biome-ignore lint/suspicious/noArrayIndexKey: Items never reordered. Using kv.key causes focus loss.
            key={i}
            className="space-y-1"
          >
            <fieldset
              className="grid grid-cols-12 gap-2 items-center border-0 p-0 m-0 min-w-0"
              onBlur={(e) => {
                // Only trigger onBlur if focus is leaving this entire row
                // relatedTarget can be null when clicking outside the document
                const relatedTarget = e.relatedTarget;

                if (
                  !(
                    relatedTarget &&
                    e.currentTarget.contains(relatedTarget as Node)
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
                className={`col-span-5 ${blockedMessage ? "border-red-500 focus-visible:ring-red-500" : ""}`}
                aria-label={`Key ${i + 1}`}
                aria-invalid={!!blockedMessage}
                disabled={disabled}
              />
              {availableInputs.length > 0 ? (
                <InputWithPlaceholders
                  value={kv.value}
                  onChange={(value) => update(i, { value })}
                  availableInputs={availableInputs}
                  placeholder="Value"
                  className="col-span-6"
                  aria-label={`Value ${i + 1}`}
                  disabled={disabled}
                />
              ) : (
                <Input
                  placeholder="Value"
                  value={kv.value}
                  onChange={(e) => update(i, { value: e.target.value })}
                  className="col-span-6"
                  aria-label={`Value ${i + 1}`}
                  disabled={disabled}
                />
              )}
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
            {blockedMessage && (
              <p className="text-red-500 text-xs ml-1">{blockedMessage}</p>
            )}
          </div>
        );
      })}
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

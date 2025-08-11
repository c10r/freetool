import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { X, Plus } from "lucide-react";
import { KeyValuePair } from "../types";

interface KeyValueListProps {
  items: KeyValuePair[] | undefined;
  onChange: (items: KeyValuePair[]) => void;
  onBlur?: (items: KeyValuePair[]) => void;
  ariaLabel?: string;
}

export default function KeyValueList({
  items = [],
  onChange,
  onBlur,
  ariaLabel,
}: KeyValueListProps) {
  const add = () => onChange([...(items || []), { key: "", value: "" }]);
  const remove = (i: number) => onChange(items.filter((_, idx) => idx !== i));
  const update = (i: number, patch: Partial<KeyValuePair>) => {
    const next = items.map((kv, idx) => (idx === i ? { ...kv, ...patch } : kv));
    onChange(next);
  };
  return (
    <div className="space-y-2" aria-label={ariaLabel}>
      {(items || []).map((kv, i) => (
        <div
          key={i}
          className="grid grid-cols-12 gap-2 items-center"
          onBlur={(e) => {
            // Only trigger onBlur if focus is leaving this entire row
            // relatedTarget can be null when clicking outside the document
            const relatedTarget = e.relatedTarget;
            if (
              !relatedTarget ||
              !e.currentTarget.contains(relatedTarget as Node)
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
          />
          <Input
            placeholder="Value"
            value={kv.value}
            onChange={(e) => update(i, { value: e.target.value })}
            className="col-span-6"
            aria-label={`Value ${i + 1}`}
          />
          <Button
            type="button"
            variant="secondary"
            size="icon"
            onClick={() => remove(i)}
            aria-label="Remove pair"
          >
            <X size={16} />
          </Button>
        </div>
      ))}
      <Button type="button" variant="secondary" onClick={add}>
        <Plus className="mr-2 h-4 w-4" /> Add pair
      </Button>
    </div>
  );
}

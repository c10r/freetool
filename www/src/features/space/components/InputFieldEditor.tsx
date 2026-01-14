import {
  ALargeSmall,
  Calendar,
  CircleDot,
  Hash,
  Mail,
  Plus,
  ToggleLeft,
  Trash2,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import type { AppField, FieldType } from "../types";
import RadioOptionsEditor from "./RadioOptionsEditor";

interface InputFieldEditorProps {
  fields: AppField[];
  onChange: (fields: AppField[]) => void;
  disabled?: boolean;
  showAddButton?: boolean;
}

/**
 * Reusable component for editing app input fields.
 * Used in both app creation (FolderView) and app editing (AppView).
 */
export default function InputFieldEditor({
  fields,
  onChange,
  disabled = false,
  showAddButton = true,
}: InputFieldEditorProps) {
  const addField = () => {
    const newField: AppField = {
      id: crypto.randomUUID(),
      label: "",
      type: "text",
      required: false,
    };
    onChange([...fields, newField]);
  };

  const updateField = (id: string, updates: Partial<AppField>) => {
    onChange(
      fields.map((f) => {
        if (f.id !== id) {
          return f;
        }
        // Initialize default options when switching to radio type
        if (updates.type === "radio" && !f.options?.length) {
          return {
            ...f,
            ...updates,
            options: [{ value: "option1" }, { value: "option2" }],
          };
        }
        return { ...f, ...updates };
      })
    );
  };

  const deleteField = (id: string) => {
    onChange(fields.filter((f) => f.id !== id));
  };

  return (
    <div className="space-y-4">
      {showAddButton && (
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">Input Fields</Label>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={addField}
            disabled={disabled}
          >
            <Plus className="mr-2 h-4 w-4" /> Add Field
          </Button>
        </div>
      )}

      {fields.length === 0 && (
        <p className="text-sm text-muted-foreground">
          No input fields defined. Add fields to create dynamic inputs that
          users can fill in when running this app. Use {"{fieldLabel}"} in URL
          path, headers, or body to reference field values.
        </p>
      )}

      {fields.map((f) => (
        <Card key={f.id}>
          <CardContent className="py-4 grid grid-cols-1 md:grid-cols-12 gap-3 items-center">
            <div className="md:col-span-4">
              <Input
                value={f.label}
                onChange={(e) => updateField(f.id, { label: e.target.value })}
                placeholder="Field label"
                aria-label="Field label"
                disabled={disabled}
              />
            </div>
            <div className="md:col-span-3">
              <Select
                value={f.type}
                onValueChange={(v: FieldType) => updateField(f.id, { type: v })}
                disabled={disabled}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="text">
                    <span className="flex items-center gap-2">
                      <ALargeSmall className="h-4 w-4" /> Text
                    </span>
                  </SelectItem>
                  <SelectItem value="email">
                    <span className="flex items-center gap-2">
                      <Mail className="h-4 w-4" /> Email
                    </span>
                  </SelectItem>
                  <SelectItem value="date">
                    <span className="flex items-center gap-2">
                      <Calendar className="h-4 w-4" /> Date
                    </span>
                  </SelectItem>
                  <SelectItem value="integer">
                    <span className="flex items-center gap-2">
                      <Hash className="h-4 w-4" /> Integer
                    </span>
                  </SelectItem>
                  <SelectItem value="boolean">
                    <span className="flex items-center gap-2">
                      <ToggleLeft className="h-4 w-4" /> Boolean
                    </span>
                  </SelectItem>
                  <SelectItem value="radio">
                    <span className="flex items-center gap-2">
                      <CircleDot className="h-4 w-4" /> Radio
                    </span>
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="md:col-span-3 flex items-center gap-2">
              <Switch
                checked={!!f.required}
                onCheckedChange={(v) => updateField(f.id, { required: v })}
                disabled={disabled}
              />
              <span className="text-sm text-muted-foreground">Required</span>
            </div>
            <div className="md:col-span-2 flex justify-end">
              <Button
                type="button"
                variant="secondary"
                size="icon"
                onClick={() => deleteField(f.id)}
                aria-label="Delete field"
                disabled={disabled}
              >
                <Trash2 size={16} />
              </Button>
            </div>
            {f.type === "radio" && (
              <div className="md:col-span-12 mt-2">
                <RadioOptionsEditor
                  options={f.options || []}
                  onChange={(opts) => updateField(f.id, { options: opts })}
                  disabled={disabled}
                />
              </div>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

import { AlertCircle, CheckCircle2 } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { validateExpression } from "@/lib/expression-evaluator";
import { Alert, AlertDescription } from "../alert";
import { Button } from "../button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "../dialog";
import type { AppInput } from "../input-with-placeholders.types";
import { InputWithPlaceholders } from "./InputWithPlaceholders";

interface ExpressionEditorProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialExpression?: string;
  availableInputs: AppInput[];
  onSave: (expression: string) => void;
  mode: "insert" | "edit";
}

export function ExpressionEditor({
  open,
  onOpenChange,
  initialExpression = "",
  availableInputs,
  onSave,
  mode,
}: ExpressionEditorProps) {
  const [expression, setExpression] = useState(initialExpression);

  // Sync expression state when dialog opens with new initial value
  // Note: handleOpenChange below only fires for user interactions (clicking overlay, Escape),
  // not when the `open` prop changes programmatically, so we need this effect
  useEffect(() => {
    if (open) {
      setExpression(initialExpression);
    }
  }, [open, initialExpression]);

  // Reset expression when dialog opens with new initial value
  const handleOpenChange = useCallback(
    (newOpen: boolean) => {
      if (newOpen) {
        setExpression(initialExpression);
      }
      onOpenChange(newOpen);
    },
    [initialExpression, onOpenChange]
  );

  // Validate expression
  const validation = useMemo(() => {
    if (!expression.trim()) {
      return { isValid: false, errors: ["Expression cannot be empty"] };
    }
    const availableVars = availableInputs
      .map((i) => i.title)
      .filter((t): t is string => t !== null && t !== undefined);
    return validateExpression(expression, availableVars);
  }, [expression, availableInputs]);

  const handleSave = useCallback(() => {
    if (validation.isValid) {
      onSave(expression.trim());
      onOpenChange(false);
    }
  }, [expression, validation.isValid, onSave, onOpenChange]);

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>
            {mode === "insert" ? "Insert Expression" : "Edit Expression"}
          </DialogTitle>
          <DialogDescription>
            Write a JavaScript-like expression or a JSON object/array. Use
            @VariableName to reference inputs.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Expression input */}
          <div className="space-y-2">
            <label htmlFor="expression" className="text-sm font-medium">
              Expression
            </label>
            <InputWithPlaceholders
              id="expression"
              value={expression}
              onChange={setExpression}
              availableInputs={availableInputs}
              placeholder="Type @ to insert variables, e.g. @Debit ? -1 * @Amount : @Amount"
              disableExpressions
            />
          </div>

          {/* Validation status */}
          {expression.trim() && (
            <Alert variant={validation.isValid ? "default" : "destructive"}>
              {validation.isValid ? (
                <CheckCircle2 className="h-4 w-4" />
              ) : (
                <AlertCircle className="h-4 w-4" />
              )}
              <AlertDescription>
                {validation.isValid
                  ? "Expression is valid"
                  : validation.errors.join(", ")}
              </AlertDescription>
            </Alert>
          )}

          {/* Syntax help */}
          <div className="text-xs text-muted-foreground space-y-1">
            <p className="font-medium">Supported operations:</p>
            <ul className="list-disc list-inside space-y-0.5">
              <li>
                Arithmetic: <code>+</code>, <code>-</code>, <code>*</code>,{" "}
                <code>/</code>, <code>%</code>
              </li>
              <li>
                Comparison: <code>==</code>, <code>!=</code>, <code>&lt;</code>,{" "}
                <code>&gt;</code>, <code>&lt;=</code>, <code>&gt;=</code>
              </li>
              <li>
                Logical: <code>&amp;&amp;</code>, <code>||</code>,{" "}
                <code>!</code>
              </li>
              <li>
                Ternary: <code>condition ? valueIfTrue : valueIfFalse</code>
              </li>
              <li>
                JSON: <code>{'{ "key": @Var }'}</code>,{" "}
                <code>{"[ @Var, 1 ]"}</code> (use @Var as a JSON value)
              </li>
            </ul>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={!validation.isValid}>
            {mode === "insert" ? "Insert" : "Save"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

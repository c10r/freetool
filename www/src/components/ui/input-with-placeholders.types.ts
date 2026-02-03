export interface AppInput {
  title?: string | null;
  required?: boolean;
}

export interface TextSegment {
  type: "text";
  content: string;
}

export interface PlaceholderSegment {
  type: "placeholder";
  inputTitle: string;
  isValid: boolean;
}

export interface ExpressionSegment {
  type: "expression";
  expression: string;
  isValid: boolean;
}

export type Segment = TextSegment | PlaceholderSegment | ExpressionSegment;

export interface PopoverState {
  isOpen: boolean;
  triggerType: "insert" | "edit";
  editingSegmentIndex?: number;
}

export interface InputWithPlaceholdersProps {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  availableInputs: AppInput[];
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  inputClassName?: string;
  id?: string;
  "aria-label"?: string;
  /** Disable {{ expression }} syntax - only allow @placeholder references */
  disableExpressions?: boolean;
  /** Optional syntax highlighting mode */
  syntaxHighlighting?: "sql";
}

import type {
  FieldType,
  RadioOption,
  SupportedCurrency,
} from "@/features/space/types";
import type { components } from "@/schema";

type InputType = components["schemas"]["InputType"];

/**
 * F# discriminated union format for InputTypeDto.
 * JsonFSharpConverter expects { case: "CaseName" } for parameterless cases
 * and { case: "CaseName", fields: [...] } for cases with parameters.
 */
interface InputTypeDtoWrite {
  case: string;
  fields?: unknown[];
}

/**
 * Maps a frontend FieldType string to a backend InputTypeDto object.
 * Uses the F# discriminated union format expected by JsonFSharpConverter.
 *
 * @param frontendType - The frontend field type string
 * @param options - Optional radio options (only used for radio type)
 * @returns An InputTypeDto object suitable for sending to the backend
 */
export function toBackendInputType(
  frontendType: FieldType,
  options?: RadioOption[],
  currency: SupportedCurrency = "USD"
): InputTypeDtoWrite {
  switch (frontendType) {
    case "email":
      return { case: "Email" };
    case "date":
      return { case: "Date" };
    case "integer":
      return { case: "Integer" };
    case "boolean":
      return { case: "Boolean" };
    case "currency":
      return { case: "Currency", fields: [{ case: currency }] };
    case "radio": {
      const backendOptions = (options || []).map((opt) => ({
        Value: opt.value,
        Label: opt.label ?? null,
      }));
      return { case: "Radio", fields: [backendOptions] };
    }
    default:
      // Text type requires a maxLength parameter
      return { case: "Text", fields: [500] };
  }
}

/**
 * Maps a backend InputType object to a frontend FieldType string.
 * Examines the `is*` boolean flags to determine the type.
 *
 * @param backendType - The InputType object from the backend
 * @returns A FieldType string for frontend use
 */
export function fromBackendInputType(backendType: InputType): FieldType {
  // The backend sends { Case: "Email" } or { Case: "Text", Fields: [500] }
  const caseName = (backendType as unknown as { Case?: string }).Case;

  if (caseName) {
    switch (caseName) {
      case "Email":
        return "email";
      case "Date":
        return "date";
      case "Text":
        return "text";
      case "Integer":
        return "integer";
      case "Boolean":
        return "boolean";
      case "Currency":
        return "currency";
      case "MultiEmail":
        return "email";
      case "MultiDate":
        return "date";
      case "MultiText":
        return "text";
      case "MultiInteger":
        return "integer";
      case "Radio":
        return "radio";
    }
  }

  // Default to text if unknown
  return "text";
}

/**
 * Extracts radio options from a backend InputType object.
 * Only returns options if the type is Radio.
 *
 * @param backendType - The InputType object from the backend
 * @returns Array of RadioOption objects, or empty array if not a Radio type
 */
export function getRadioOptionsFromBackendType(
  backendType: InputType
): RadioOption[] {
  const typedBackend = backendType as unknown as {
    Case?: string;
    Fields?: unknown[];
  };

  if (typedBackend.Case !== "Radio" || !typedBackend.Fields) {
    return [];
  }

  // Handle both camelCase (from API response) and PascalCase formats
  const backendOptions = typedBackend.Fields[0] as Array<{
    value?: string;
    Value?: string;
    label?: string | null;
    Label?: string | null;
  }>;

  return backendOptions.map((opt) => ({
    value: opt.value ?? opt.Value ?? "",
    label: opt.label ?? opt.Label ?? undefined,
  }));
}

export function getCurrencyFromBackendType(
  backendType: InputType
): SupportedCurrency {
  const typedBackend = backendType as unknown as {
    Case?: string;
    Fields?: unknown[];
  };

  if (typedBackend.Case !== "Currency" || !typedBackend.Fields?.length) {
    return "USD";
  }

  const currencyField = typedBackend.Fields[0] as
    | { Case?: string; case?: string }
    | string;

  const currencyCase =
    typeof currencyField === "string"
      ? currencyField
      : currencyField.Case || currencyField.case;

  return currencyCase === "USD" ? "USD" : "USD";
}

/**
 * Extracts the display value from a backend DefaultValue discriminated union object.
 * The backend returns default values in F# DU format:
 * { "Case": "IntegerDefault", "Fields": [600] }
 *
 * @param defaultValue - The DefaultValue object from the backend (or undefined)
 * @returns The extracted value as a display string, or null if no default value
 */
export function extractDefaultValue(defaultValue: unknown): string | null {
  if (!defaultValue) {
    return null;
  }

  const typed = defaultValue as { Case?: string; Fields?: unknown[] };
  if (!(typed.Case && typed.Fields) || typed.Fields.length === 0) {
    return null;
  }

  const value = typed.Fields[0];

  switch (typed.Case) {
    case "IntegerDefault":
    case "TextDefault":
    case "RadioDefault":
    case "EmailDefault":
      return String(value);
    case "CurrencyDefault":
      return String(value);
    case "BooleanDefault":
      return value ? "true" : "false";
    case "DateDefault":
      return new Date(value as string).toLocaleDateString();
    default:
      return String(value);
  }
}

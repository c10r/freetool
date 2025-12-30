import type { FieldType } from "@/features/space/types";
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
 * F# discriminated union tag values for InputTypeDto.
 * These correspond to the order of cases in the F# DU:
 * Email = 0, Date = 1, Text = 2, Integer = 3, Boolean = 4, etc.
 * Used for reading/deserializing backend responses.
 */
const INPUT_TYPE_TAGS = {
  Email: 0,
  Date: 1,
  Text: 2,
  Integer: 3,
  Boolean: 4,
  MultiEmail: 5,
  MultiDate: 6,
  MultiText: 7,
  MultiInteger: 8,
} as const;

/**
 * Maps a frontend FieldType string to a backend InputTypeDto object.
 * Uses the F# discriminated union format expected by JsonFSharpConverter.
 *
 * @param frontendType - The frontend field type string
 * @returns An InputTypeDto object suitable for sending to the backend
 */
export function toBackendInputType(frontendType: FieldType): InputTypeDtoWrite {
  switch (frontendType) {
    case "email":
      return { case: "Email" };
    case "date":
      return { case: "Date" };
    case "integer":
      return { case: "Integer" };
    case "boolean":
      return { case: "Boolean" };
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
  // Check the item/value property which contains the InputTypeValue
  const typeValue = backendType.item ?? backendType.value;

  if (typeValue) {
    if (typeValue.isEmail) {
      return "email";
    }
    if (typeValue.isDate) {
      return "date";
    }
    if (typeValue.isText) {
      return "text";
    }
    if (typeValue.isInteger) {
      return "integer";
    }
    if (typeValue.isBoolean) {
      return "boolean";
    }
    // Multi-types are not supported as frontend FieldTypes, default to text
    if (typeValue.isMultiEmail) {
      return "email";
    }
    if (typeValue.isMultiDate) {
      return "date";
    }
    if (typeValue.isMultiText) {
      return "text";
    }
    if (typeValue.isMultiInteger) {
      return "integer";
    }
  }

  // Fallback: check tag value
  if (backendType.tag !== undefined) {
    switch (backendType.tag) {
      case INPUT_TYPE_TAGS.Email:
        return "email";
      case INPUT_TYPE_TAGS.Date:
        return "date";
      case INPUT_TYPE_TAGS.Text:
        return "text";
      case INPUT_TYPE_TAGS.Integer:
        return "integer";
      case INPUT_TYPE_TAGS.Boolean:
        return "boolean";
      case INPUT_TYPE_TAGS.MultiEmail:
        return "email";
      case INPUT_TYPE_TAGS.MultiDate:
        return "date";
      case INPUT_TYPE_TAGS.MultiText:
        return "text";
      case INPUT_TYPE_TAGS.MultiInteger:
        return "integer";
    }
  }

  // Default to text if unknown
  return "text";
}

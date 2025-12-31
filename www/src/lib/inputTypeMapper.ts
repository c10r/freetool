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
      case "MultiEmail":
        return "email";
      case "MultiDate":
        return "date";
      case "MultiText":
        return "text";
      case "MultiInteger":
        return "integer";
    }
  }

  // Default to text if unknown
  return "text";
}

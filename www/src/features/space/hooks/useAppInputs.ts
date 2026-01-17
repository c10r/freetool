import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { updateAppInputSchema } from "@/api/api";
import { toBackendInputType } from "@/lib/inputTypeMapper";
import type { AppField } from "../types";

interface InputsState {
  updating: boolean;
  saved: boolean;
  error: boolean;
  errorMessage: string;
}

const initialInputsState: InputsState = {
  updating: false,
  saved: false,
  error: false,
  errorMessage: "",
};

/**
 * Hook for managing app inputs (fields) with manual save functionality.
 * Changes are tracked locally and saved explicitly via saveInputs().
 *
 * @param initialFields - Initial app fields to manage
 * @param appId - Optional app ID for API calls
 * @param onUpdate - Optional callback when fields are updated successfully
 */
export function useAppInputs(
  initialFields: AppField[],
  appId?: string,
  onUpdate?: (updatedFields: AppField[]) => void
) {
  const [fields, setFields] = useState<AppField[]>(initialFields);
  const [inputsState, setInputsState] =
    useState<InputsState>(initialInputsState);
  const savedFieldsRef = useRef<AppField[]>(initialFields);

  // Sync with initial fields when they change (e.g., when app changes)
  useEffect(() => {
    savedFieldsRef.current = initialFields;
    setFields(initialFields);
  }, [initialFields]);

  const resetState = useCallback(() => {
    setInputsState(initialInputsState);
  }, []);

  /**
   * Maps frontend AppField[] to backend AppInputDto[] format
   */
  const mapFieldsToBackendFormat = useCallback((appFields: AppField[]) => {
    return appFields.map((field) => ({
      input: {
        title: field.label,
        description: field.description?.trim() || null,
        type: toBackendInputType(field.type, field.options),
      },
      required: field.required ?? false,
      defaultValue: field.defaultValue,
    }));
  }, []);

  /**
   * Check if there are unsaved changes by comparing current fields with saved fields
   */
  const hasUnsavedChanges = useMemo(() => {
    const currentFieldsJson = JSON.stringify(
      fields.map((f) => ({
        label: f.label,
        description: f.description,
        type: f.type,
        required: f.required ?? false,
        options: f.options,
        defaultValue: f.defaultValue,
      }))
    );
    const savedFieldsJson = JSON.stringify(
      savedFieldsRef.current.map((f) => ({
        label: f.label,
        description: f.description,
        type: f.type,
        required: f.required ?? false,
        options: f.options,
        defaultValue: f.defaultValue,
      }))
    );
    return currentFieldsJson !== savedFieldsJson;
  }, [fields]);

  /**
   * Performs the save operation to the backend
   */
  const saveInputs = useCallback(async () => {
    if (!appId) {
      return { success: false, error: "No app ID" };
    }

    // Check if fields have actually changed
    if (!hasUnsavedChanges) {
      return { success: true };
    }

    setInputsState((prev) => ({
      ...prev,
      updating: true,
      saved: false,
      error: false,
    }));

    try {
      const backendInputs = mapFieldsToBackendFormat(fields);
      const response = await updateAppInputSchema(appId, backendInputs);

      if (response.error) {
        const errorMessage =
          (response.error as { message?: string }).message ||
          "Failed to save fields";
        setInputsState({
          updating: false,
          saved: false,
          error: true,
          errorMessage,
        });

        // Clear error after 2 seconds
        setTimeout(() => {
          setInputsState((prev) => ({
            ...prev,
            error: false,
            errorMessage: "",
          }));
        }, 2000);

        return { success: false, error: errorMessage };
      }

      // Update saved reference on success
      savedFieldsRef.current = fields;
      onUpdate?.(fields);

      setInputsState({
        updating: false,
        saved: true,
        error: false,
        errorMessage: "",
      });

      // Clear saved indicator after 2 seconds
      setTimeout(() => {
        setInputsState((prev) => ({ ...prev, saved: false }));
      }, 2000);

      return { success: true };
    } catch (_error) {
      setInputsState({
        updating: false,
        saved: false,
        error: true,
        errorMessage: "Network error occurred",
      });

      // Clear error after 2 seconds
      setTimeout(() => {
        setInputsState((prev) => ({
          ...prev,
          error: false,
          errorMessage: "",
        }));
      }, 2000);

      return { success: false, error: "Network error occurred" };
    }
  }, [appId, fields, hasUnsavedChanges, mapFieldsToBackendFormat, onUpdate]);

  /**
   * Discards unsaved changes and resets to saved state
   */
  const discardChanges = useCallback(() => {
    setFields(savedFieldsRef.current);
  }, []);

  /**
   * Adds a new field (no auto-save)
   */
  const addField = useCallback(() => {
    const id = crypto.randomUUID();
    const newField: AppField = {
      id,
      label: `Field ${fields.length + 1}`,
      type: "text",
    };
    setFields((prev) => [...prev, newField]);
  }, [fields.length]);

  /**
   * Updates an existing field (no auto-save)
   */
  const updateField = useCallback((id: string, patch: Partial<AppField>) => {
    setFields((prev) =>
      prev.map((f) => (f.id === id ? { ...f, ...patch } : f))
    );
  }, []);

  /**
   * Deletes a field (no auto-save)
   */
  const deleteField = useCallback((id: string) => {
    setFields((prev) => prev.filter((f) => f.id !== id));
  }, []);

  return {
    fields,
    inputsState,
    hasUnsavedChanges,
    addField,
    updateField,
    deleteField,
    resetState,
    setFields,
    saveInputs,
    discardChanges,
  };
}

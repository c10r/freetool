import { useCallback, useEffect, useRef, useState } from "react";
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
 * Hook for managing app inputs (fields) with autosave functionality.
 * Follows the same pattern as useAppForm for consistency.
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
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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
        type: toBackendInputType(field.type),
      },
      required: field.required ?? false,
    }));
  }, []);

  /**
   * Performs the actual save operation to the backend
   */
  const saveFields = useCallback(
    async (fieldsToSave: AppField[]) => {
      if (!appId) {
        return;
      }

      // Check if fields have actually changed
      const currentFieldsJson = JSON.stringify(
        fieldsToSave.map((f) => ({
          label: f.label,
          type: f.type,
          required: f.required ?? false,
        }))
      );
      const savedFieldsJson = JSON.stringify(
        savedFieldsRef.current.map((f) => ({
          label: f.label,
          type: f.type,
          required: f.required ?? false,
        }))
      );

      if (currentFieldsJson === savedFieldsJson) {
        return;
      }

      setInputsState((prev) => ({
        ...prev,
        updating: true,
        saved: false,
        error: false,
      }));

      try {
        const backendInputs = mapFieldsToBackendFormat(fieldsToSave);
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

          // Reset to saved state on error
          setFields(savedFieldsRef.current);

          // Clear error after 2 seconds
          setTimeout(() => {
            setInputsState((prev) => ({
              ...prev,
              error: false,
              errorMessage: "",
            }));
          }, 2000);

          return;
        }

        // Update saved reference on success
        savedFieldsRef.current = fieldsToSave;
        onUpdate?.(fieldsToSave);

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
      } catch (_error) {
        setInputsState({
          updating: false,
          saved: false,
          error: true,
          errorMessage: "Network error occurred",
        });

        // Reset to saved state on error
        setFields(savedFieldsRef.current);

        // Clear error after 2 seconds
        setTimeout(() => {
          setInputsState((prev) => ({
            ...prev,
            error: false,
            errorMessage: "",
          }));
        }, 2000);
      }
    },
    [appId, mapFieldsToBackendFormat, onUpdate]
  );

  /**
   * Triggers a debounced save operation
   */
  const triggerSave = useCallback(
    (fieldsToSave: AppField[]) => {
      // Clear any existing debounce timer
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }

      // Set new debounce timer (500ms delay)
      debounceTimerRef.current = setTimeout(() => {
        saveFields(fieldsToSave);
      }, 500);
    },
    [saveFields]
  );

  /**
   * Adds a new field and triggers autosave
   */
  const addField = useCallback(() => {
    const id = crypto.randomUUID();
    const newField: AppField = {
      id,
      label: `Field ${fields.length + 1}`,
      type: "text",
    };
    const updatedFields = [...fields, newField];
    setFields(updatedFields);
    triggerSave(updatedFields);
  }, [fields, triggerSave]);

  /**
   * Updates an existing field and triggers autosave
   */
  const updateField = useCallback(
    (id: string, patch: Partial<AppField>) => {
      const updatedFields = fields.map((f) =>
        f.id === id ? { ...f, ...patch } : f
      );
      setFields(updatedFields);
      triggerSave(updatedFields);
    },
    [fields, triggerSave]
  );

  /**
   * Deletes a field and triggers autosave
   */
  const deleteField = useCallback(
    (id: string) => {
      const updatedFields = fields.filter((f) => f.id !== id);
      setFields(updatedFields);
      triggerSave(updatedFields);
    },
    [fields, triggerSave]
  );

  /**
   * Manually triggers an immediate save (bypasses debounce)
   */
  const saveNow = useCallback(() => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = null;
    }
    saveFields(fields);
  }, [fields, saveFields]);

  // Cleanup debounce timer on unmount
  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, []);

  return {
    fields,
    inputsState,
    addField,
    updateField,
    deleteField,
    resetState,
    setFields,
    saveNow,
    triggerSave,
  };
}

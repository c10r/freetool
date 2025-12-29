import { useCallback, useEffect, useRef, useState } from "react";
import {
  updateResourceBaseUrl,
  updateResourceBody,
  updateResourceDescription,
  updateResourceHeaders,
  updateResourceName,
  updateResourceUrlParameters,
} from "@/api/api";
import type { KeyValuePair } from "../types";

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface ResourceFormData {
  name: string;
  description: string;
  baseUrl: string;
  httpMethod: HttpMethod;
  urlParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
}

interface FieldState {
  updating: boolean;
  saved: boolean;
  error: boolean;
  errorMessage: string;
}

type FieldStates = {
  name: FieldState;
  description: FieldState;
  baseUrl: FieldState;
  urlParameters: FieldState;
  headers: FieldState;
  body: FieldState;
};

const initialFieldState: FieldState = {
  updating: false,
  saved: false,
  error: false,
  errorMessage: "",
};

const initialFieldStates: FieldStates = {
  name: initialFieldState,
  description: initialFieldState,
  baseUrl: initialFieldState,
  urlParameters: initialFieldState,
  headers: initialFieldState,
  body: initialFieldState,
};

export function useResourceForm(
  initialData: ResourceFormData,
  resourceId?: string,
  onUpdate?: (updatedData: ResourceFormData) => void
) {
  const [formData, setFormData] = useState<ResourceFormData>(initialData);
  const [fieldStates, setFieldStates] =
    useState<FieldStates>(initialFieldStates);
  const savedDataRef = useRef<ResourceFormData>(initialData);

  useEffect(() => {
    savedDataRef.current = initialData;
  }, [initialData]);

  const resetFieldStates = useCallback(() => {
    setFieldStates(initialFieldStates);
  }, []);

  const setFieldState = useCallback(
    (field: keyof FieldStates, state: Partial<FieldState>) => {
      setFieldStates((prev) => ({
        ...prev,
        [field]: { ...prev[field], ...state },
      }));
    },
    []
  );

  const updateFormData = useCallback(
    (
      field: keyof ResourceFormData,
      value: string | HttpMethod | KeyValuePair[]
    ) => {
      setFormData((prev) => ({ ...prev, [field]: value }));
    },
    []
  );

  const updateTextField = useCallback(
    async (field: "name" | "description" | "baseUrl", value: string) => {
      if (!(resourceId && value.trim())) {
        return;
      }

      // Don't make network request if value hasn't changed
      if (value.trim() === savedDataRef.current[field]) {
        return;
      }

      setFieldState(field, { updating: true, saved: false, error: false });

      try {
        let response:
          | Awaited<ReturnType<typeof updateResourceName>>
          | Awaited<ReturnType<typeof updateResourceDescription>>
          | Awaited<ReturnType<typeof updateResourceBaseUrl>>
          | undefined;
        if (field === "name") {
          response = await updateResourceName({
            id: resourceId,
            newName: value.trim(),
          });
        } else if (field === "description") {
          response = await updateResourceDescription({
            id: resourceId,
            newDescription: value.trim(),
          });
        } else if (field === "baseUrl") {
          response = await updateResourceBaseUrl({
            id: resourceId,
            baseUrl: value.trim(),
          });
        }

        if (response?.error) {
          const errorMessage = response.error.message || "Failed to save";
          setFieldState(field, {
            updating: false,
            error: true,
            errorMessage,
          });

          // Reset the field value on error
          setFormData((prev) => ({
            ...prev,
            [field]: savedDataRef.current[field],
          }));

          // Hide error indicator after 2 seconds
          setTimeout(() => {
            setFieldState(field, { error: false, errorMessage: "" });
          }, 2000);

          return;
        }

        // Update local state on success
        const updatedData = { ...formData, [field]: value.trim() };
        savedDataRef.current = {
          ...savedDataRef.current,
          [field]: value.trim(),
        };
        setFormData(updatedData);
        onUpdate?.(updatedData);

        setFieldState(field, { updating: false, saved: true });
        setTimeout(() => {
          setFieldState(field, { saved: false });
        }, 2000);
      } catch (_error) {
        setFieldState(field, {
          updating: false,
          error: true,
          errorMessage: "Network error occurred",
        });

        // Reset the field value on error
        setFormData((prev) => ({
          ...prev,
          [field]: savedDataRef.current[field],
        }));

        setTimeout(() => {
          setFieldState(field, { error: false, errorMessage: "" });
        }, 2000);
      }
    },
    [resourceId, onUpdate, setFieldState, formData]
  );

  const updateKeyValueField = useCallback(
    async (
      field: "urlParameters" | "headers" | "body",
      value: KeyValuePair[]
    ) => {
      if (!resourceId) {
        return;
      }

      // Filter out empty key-value pairs
      const filteredValue = value.filter(
        (pair) => pair.key.trim() !== "" && pair.value.trim() !== ""
      );

      // Don't make network request if value hasn't changed
      const currentValue = (savedDataRef.current[field] || []).filter(
        (pair) => pair.key.trim() !== "" && pair.value.trim() !== ""
      );
      if (JSON.stringify(filteredValue) === JSON.stringify(currentValue)) {
        return;
      }

      setFieldState(field, { updating: true, saved: false, error: false });

      try {
        let response:
          | Awaited<ReturnType<typeof updateResourceUrlParameters>>
          | Awaited<ReturnType<typeof updateResourceHeaders>>
          | Awaited<ReturnType<typeof updateResourceBody>>
          | undefined;
        if (field === "urlParameters") {
          response = await updateResourceUrlParameters({
            id: resourceId,
            urlParameters: filteredValue,
          });
        } else if (field === "headers") {
          response = await updateResourceHeaders({
            id: resourceId,
            headers: filteredValue,
          });
        } else if (field === "body") {
          response = await updateResourceBody({
            id: resourceId,
            body: filteredValue,
          });
        }

        if (response?.error) {
          const errorMessage = response.error.message || "Failed to save";
          setFieldState(field, {
            updating: false,
            error: true,
            errorMessage,
          });

          // Reset the field value on error
          setFormData((prev) => ({
            ...prev,
            [field]: savedDataRef.current[field] || [],
          }));

          setTimeout(() => {
            setFieldState(field, { error: false, errorMessage: "" });
          }, 2000);

          return;
        }

        // Update local state on success
        const updatedData = { ...formData, [field]: filteredValue };
        savedDataRef.current = {
          ...savedDataRef.current,
          [field]: filteredValue,
        };
        setFormData(updatedData);
        onUpdate?.(updatedData);

        setFieldState(field, { updating: false, saved: true });
        setTimeout(() => {
          setFieldState(field, { saved: false });
        }, 2000);
      } catch (_error) {
        setFieldState(field, {
          updating: false,
          error: true,
          errorMessage: "Network error occurred",
        });

        // Reset the field value on error
        setFormData((prev) => ({
          ...prev,
          [field]: savedDataRef.current[field] || [],
        }));

        setTimeout(() => {
          setFieldState(field, { error: false, errorMessage: "" });
        }, 2000);
      }
    },
    [resourceId, onUpdate, setFieldState, formData]
  );

  return {
    formData,
    fieldStates,
    updateFormData,
    updateTextField,
    updateKeyValueField,
    resetFieldStates,
    setFormData,
  };
}

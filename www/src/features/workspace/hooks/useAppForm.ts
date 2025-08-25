import { useState, useCallback, useRef, useEffect } from "react";
import { KeyValuePair } from "../types";
import {
  updateAppBody,
  updateAppHeaders,
  updateAppQueryParams,
  updateAppUrlPath,
} from "@/api/api";

export interface AppFormData {
  urlPath: string;
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
  urlPath: FieldState;
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
  urlPath: initialFieldState,
  urlParameters: initialFieldState,
  headers: initialFieldState,
  body: initialFieldState,
};

export function useAppForm(
  initialData: AppFormData,
  appId?: string,
  onUpdate?: (updatedData: AppFormData) => void,
) {
  const [formData, setFormData] = useState<AppFormData>(initialData);
  const [fieldStates, setFieldStates] =
    useState<FieldStates>(initialFieldStates);
  const savedDataRef = useRef<AppFormData>(initialData);

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
    [],
  );

  const updateFormData = useCallback((field: keyof AppFormData, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  }, []);

  const updateKeyValueField = useCallback(
    async (
      field: "urlParameters" | "headers" | "body",
      value: KeyValuePair[],
    ) => {
      if (!appId) {
        return;
      }

      // Filter out empty key-value pairs
      const filteredValue = value.filter(
        (pair) => pair.key.trim() !== "" && pair.value.trim() !== "",
      );

      // Don't make network request if value hasn't changed
      const currentValue = (savedDataRef.current[field] || []).filter(
        (pair) => pair.key.trim() !== "" && pair.value.trim() !== "",
      );

      if (JSON.stringify(filteredValue) === JSON.stringify(currentValue)) {
        return;
      }

      setFieldState(field, { updating: true, saved: false, error: false });

      try {
        // Create the updated data with the new field value
        const updatedData = { ...formData, [field]: filteredValue };

        let response;
        if (field === "urlParameters") {
          response = await updateAppQueryParams(appId, filteredValue);
        } else if (field === "headers") {
          response = await updateAppHeaders(appId, filteredValue);
        } else if (field === "body") {
          response = await updateAppBody(appId, filteredValue);
        }

        if (response && response.error) {
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
      } catch (error) {
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
    [appId, onUpdate, setFieldState, formData],
  );

  const updateUrlPathField = useCallback(
    async (value: string) => {
      if (!appId) {
        return;
      }

      // Don't make network request if value hasn't changed
      if (value === savedDataRef.current.urlPath) {
        return;
      }

      setFieldState("urlPath", { updating: true, saved: false, error: false });

      try {
        const response = await updateAppUrlPath(appId, value);

        if (response && response.error) {
          const errorMessage =
            response.error.message || "Failed to save URL path";
          setFieldState("urlPath", {
            updating: false,
            error: true,
            errorMessage,
          });

          // Reset the field value on error
          setFormData((prev) => ({
            ...prev,
            urlPath: savedDataRef.current.urlPath || "",
          }));

          setTimeout(() => {
            setFieldState("urlPath", { error: false, errorMessage: "" });
          }, 2000);

          return;
        }

        // Update local state on success
        const updatedData = { ...formData, urlPath: value };
        savedDataRef.current = { ...savedDataRef.current, urlPath: value };
        setFormData(updatedData);
        onUpdate?.(updatedData);

        setFieldState("urlPath", { updating: false, saved: true });
        setTimeout(() => {
          setFieldState("urlPath", { saved: false });
        }, 2000);
      } catch (error) {
        setFieldState("urlPath", {
          updating: false,
          error: true,
          errorMessage: "Network error occurred",
        });

        // Reset the field value on error
        setFormData((prev) => ({
          ...prev,
          urlPath: savedDataRef.current.urlPath || "",
        }));

        setTimeout(() => {
          setFieldState("urlPath", { error: false, errorMessage: "" });
        }, 2000);
      }
    },
    [appId, onUpdate, setFieldState, formData],
  );

  return {
    formData,
    fieldStates,
    updateFormData,
    updateKeyValueField,
    updateUrlPathField,
    resetFieldStates,
    setFormData,
  };
}

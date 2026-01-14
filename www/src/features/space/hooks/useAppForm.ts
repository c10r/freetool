import { useCallback, useMemo, useRef, useState } from "react";
import {
  updateAppBody,
  updateAppHeaders,
  updateAppHttpMethod,
  updateAppQueryParams,
  updateAppUrlPath,
  updateAppUseDynamicJsonBody,
} from "@/api/api";
import type { EndpointMethod, KeyValuePair } from "../types";

export interface AppFormData {
  httpMethod?: EndpointMethod;
  urlPath: string;
  urlParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
  useDynamicJsonBody?: boolean;
}

interface SaveState {
  saving: boolean;
  saved: boolean;
  error: boolean;
  errorMessage: string;
}

const initialSaveState: SaveState = {
  saving: false,
  saved: false,
  error: false,
  errorMessage: "",
};

/**
 * Filters out empty key-value pairs
 */
function filterEmptyPairs(pairs: KeyValuePair[]): KeyValuePair[] {
  return pairs.filter(
    (pair) => pair.key.trim() !== "" && pair.value.trim() !== ""
  );
}

/**
 * Compares two AppFormData objects for equality (used for dirty checking)
 */
function isFormDataEqual(a: AppFormData, b: AppFormData): boolean {
  if (a.httpMethod !== b.httpMethod) {
    return false;
  }
  if (a.urlPath !== b.urlPath) {
    return false;
  }
  if (a.useDynamicJsonBody !== b.useDynamicJsonBody) {
    return false;
  }

  const aParams = filterEmptyPairs(a.urlParameters);
  const bParams = filterEmptyPairs(b.urlParameters);
  if (JSON.stringify(aParams) !== JSON.stringify(bParams)) {
    return false;
  }

  const aHeaders = filterEmptyPairs(a.headers);
  const bHeaders = filterEmptyPairs(b.headers);
  if (JSON.stringify(aHeaders) !== JSON.stringify(bHeaders)) {
    return false;
  }

  const aBody = filterEmptyPairs(a.body);
  const bBody = filterEmptyPairs(b.body);
  if (JSON.stringify(aBody) !== JSON.stringify(bBody)) {
    return false;
  }

  return true;
}

export function useAppForm(
  initialData: AppFormData,
  appId?: string,
  onUpdate?: (updatedData: AppFormData) => void
) {
  const [formData, setFormData] = useState<AppFormData>(initialData);
  const [saveState, setSaveState] = useState<SaveState>(initialSaveState);
  const savedDataRef = useRef<AppFormData>(initialData);

  /**
   * Check if there are unsaved changes
   */
  const hasUnsavedChanges = useMemo(() => {
    return !isFormDataEqual(formData, savedDataRef.current);
  }, [formData]);

  /**
   * Update a single form field (no auto-save)
   */
  const updateFormData = useCallback(
    (
      field: keyof AppFormData,
      value: string | boolean | EndpointMethod | KeyValuePair[]
    ) => {
      setFormData((prev) => ({ ...prev, [field]: value }));
    },
    []
  );

  /**
   * Discards unsaved changes and resets to saved state
   */
  const discardChanges = useCallback(() => {
    setFormData(savedDataRef.current);
  }, []);

  /**
   * Save all changed config fields to the backend
   */
  const saveConfig = useCallback(async () => {
    if (!appId) {
      return { success: false, error: "No app ID" };
    }

    if (!hasUnsavedChanges) {
      return { success: true };
    }

    setSaveState({
      saving: true,
      saved: false,
      error: false,
      errorMessage: "",
    });

    const errors: string[] = [];
    const savedData = savedDataRef.current;

    try {
      // Save HTTP method if changed
      if (formData.httpMethod !== savedData.httpMethod && formData.httpMethod) {
        const response = await updateAppHttpMethod(appId, formData.httpMethod);
        if (response?.error) {
          errors.push(response.error.message || "Failed to save HTTP method");
        }
      }

      // Save URL path if changed
      if (formData.urlPath !== savedData.urlPath) {
        const response = await updateAppUrlPath(appId, formData.urlPath);
        if (response?.error) {
          errors.push(response.error.message || "Failed to save URL path");
        }
      }

      // Save URL parameters if changed
      const currentParams = filterEmptyPairs(formData.urlParameters);
      const savedParams = filterEmptyPairs(savedData.urlParameters);
      if (JSON.stringify(currentParams) !== JSON.stringify(savedParams)) {
        const response = await updateAppQueryParams(appId, currentParams);
        if (response?.error) {
          errors.push(
            response.error.message || "Failed to save URL parameters"
          );
        }
      }

      // Save headers if changed
      const currentHeaders = filterEmptyPairs(formData.headers);
      const savedHeaders = filterEmptyPairs(savedData.headers);
      if (JSON.stringify(currentHeaders) !== JSON.stringify(savedHeaders)) {
        const response = await updateAppHeaders(appId, currentHeaders);
        if (response?.error) {
          errors.push(response.error.message || "Failed to save headers");
        }
      }

      // Save body if changed
      const currentBody = filterEmptyPairs(formData.body);
      const savedBody = filterEmptyPairs(savedData.body);
      if (JSON.stringify(currentBody) !== JSON.stringify(savedBody)) {
        const response = await updateAppBody(appId, currentBody);
        if (response?.error) {
          errors.push(response.error.message || "Failed to save body");
        }
      }

      // Save useDynamicJsonBody if changed
      if (formData.useDynamicJsonBody !== savedData.useDynamicJsonBody) {
        const response = await updateAppUseDynamicJsonBody(
          appId,
          formData.useDynamicJsonBody ?? false
        );
        if (response?.error) {
          errors.push(
            response.error.message || "Failed to save dynamic JSON body setting"
          );
        }
      }

      if (errors.length > 0) {
        setSaveState({
          saving: false,
          saved: false,
          error: true,
          errorMessage: errors.join(", "),
        });

        // Clear error after 3 seconds
        setTimeout(() => {
          setSaveState((prev) => ({ ...prev, error: false, errorMessage: "" }));
        }, 3000);

        return { success: false, error: errors.join(", ") };
      }

      // Update saved reference on success
      savedDataRef.current = formData;
      onUpdate?.(formData);

      setSaveState({
        saving: false,
        saved: true,
        error: false,
        errorMessage: "",
      });

      // Clear saved indicator after 2 seconds
      setTimeout(() => {
        setSaveState((prev) => ({ ...prev, saved: false }));
      }, 2000);

      return { success: true };
    } catch (_error) {
      setSaveState({
        saving: false,
        saved: false,
        error: true,
        errorMessage: "Network error occurred",
      });

      // Clear error after 3 seconds
      setTimeout(() => {
        setSaveState((prev) => ({ ...prev, error: false, errorMessage: "" }));
      }, 3000);

      return { success: false, error: "Network error occurred" };
    }
  }, [appId, formData, hasUnsavedChanges, onUpdate]);

  return {
    formData,
    saveState,
    hasUnsavedChanges,
    updateFormData,
    saveConfig,
    discardChanges,
    setFormData,
  };
}

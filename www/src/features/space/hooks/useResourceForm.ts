import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  updateResourceBaseUrl,
  updateResourceBody,
  updateResourceDatabaseConfig,
  updateResourceDescription,
  updateResourceHeaders,
  updateResourceName,
  updateResourceUrlParameters,
} from "@/api/api";
import type {
  AuthConfig,
  DatabaseConfig,
  KeyValuePair,
  ResourceKind,
} from "../types";
import { injectAuthIntoHeaders } from "../utils/authUtils";

export interface ResourceFormData {
  name: string;
  description: string;
  resourceKind: ResourceKind;
  baseUrl: string;
  urlParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
  authConfig: AuthConfig;
  databaseConfig: DatabaseConfig;
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

function isDatabaseConfigEqual(a: DatabaseConfig, b: DatabaseConfig): boolean {
  if (a.databaseName.trim() !== b.databaseName.trim()) {
    return false;
  }
  if (a.host.trim() !== b.host.trim()) {
    return false;
  }
  if (a.port.trim() !== b.port.trim()) {
    return false;
  }
  if (a.engine !== b.engine) {
    return false;
  }
  if (a.authScheme !== b.authScheme) {
    return false;
  }
  if (a.username.trim() !== b.username.trim()) {
    return false;
  }
  if (a.password.trim() !== b.password.trim()) {
    return false;
  }
  if (a.useSsl !== b.useSsl) {
    return false;
  }
  if (a.enableSshTunnel !== b.enableSshTunnel) {
    return false;
  }

  const aOptions = filterEmptyPairs(a.connectionOptions);
  const bOptions = filterEmptyPairs(b.connectionOptions);
  return JSON.stringify(aOptions) === JSON.stringify(bOptions);
}

/**
 * Compares two ResourceFormData objects for equality (used for dirty checking)
 */
function isFormDataEqual(a: ResourceFormData, b: ResourceFormData): boolean {
  if (a.name !== b.name) {
    return false;
  }
  if (a.description !== b.description) {
    return false;
  }
  if (a.resourceKind !== b.resourceKind) {
    return false;
  }
  if (a.baseUrl !== b.baseUrl) {
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

  // Compare auth config
  if (JSON.stringify(a.authConfig) !== JSON.stringify(b.authConfig)) {
    return false;
  }

  if (!isDatabaseConfigEqual(a.databaseConfig, b.databaseConfig)) {
    return false;
  }

  return true;
}

export function useResourceForm(
  initialData: ResourceFormData,
  resourceId?: string,
  onUpdate?: (updatedData: ResourceFormData) => void
) {
  const [formData, setFormData] = useState<ResourceFormData>(initialData);
  const [saveState, setSaveState] = useState<SaveState>(initialSaveState);
  const savedDataRef = useRef<ResourceFormData>(initialData);

  // Sync with initial data when it changes (e.g., when resource changes)
  useEffect(() => {
    savedDataRef.current = initialData;
    setFormData(initialData);
  }, [initialData]);

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
      field: keyof ResourceFormData,
      value: ResourceFormData[keyof ResourceFormData]
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
   * Resets both form data and saved baseline (for initializing edit mode)
   */
  const resetFormData = useCallback((data: ResourceFormData) => {
    setFormData(data);
    savedDataRef.current = data;
  }, []);

  /**
   * Save all changed config fields to the backend
   */
  const saveConfig = useCallback(async () => {
    if (!resourceId) {
      return { success: false, error: "No resource ID" };
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
      // Save name if changed
      if (formData.name !== savedData.name && formData.name.trim()) {
        const response = await updateResourceName({
          id: resourceId,
          newName: formData.name.trim(),
        });
        if (response?.error) {
          errors.push(response.error.message || "Failed to save name");
        }
      }

      // Save description if changed
      if (formData.description !== savedData.description) {
        const response = await updateResourceDescription({
          id: resourceId,
          newDescription: formData.description.trim(),
        });
        if (response?.error) {
          errors.push(response.error.message || "Failed to save description");
        }
      }

      if (formData.resourceKind === "http") {
        // Save baseUrl if changed
        if (formData.baseUrl !== savedData.baseUrl && formData.baseUrl.trim()) {
          const response = await updateResourceBaseUrl({
            id: resourceId,
            baseUrl: formData.baseUrl.trim(),
          });
          if (response?.error) {
            errors.push(response.error.message || "Failed to save base URL");
          }
        }

        // Save URL parameters if changed
        const currentParams = filterEmptyPairs(formData.urlParameters);
        const savedParams = filterEmptyPairs(savedData.urlParameters);
        if (JSON.stringify(currentParams) !== JSON.stringify(savedParams)) {
          const response = await updateResourceUrlParameters({
            id: resourceId,
            urlParameters: currentParams,
          });
          if (response?.error) {
            errors.push(
              response.error.message || "Failed to save URL parameters"
            );
          }
        }

        // Save headers if changed (including auth injection)
        // Inject auth into headers to create the full headers array
        const headersWithAuth = injectAuthIntoHeaders(
          formData.headers,
          formData.authConfig
        );
        const currentHeaders = filterEmptyPairs(headersWithAuth);
        const savedHeadersWithAuth = injectAuthIntoHeaders(
          savedData.headers,
          savedData.authConfig
        );
        const savedHeaders = filterEmptyPairs(savedHeadersWithAuth);
        if (JSON.stringify(currentHeaders) !== JSON.stringify(savedHeaders)) {
          const response = await updateResourceHeaders({
            id: resourceId,
            headers: currentHeaders,
          });
          if (response?.error) {
            errors.push(response.error.message || "Failed to save headers");
          }
        }

        // Save body if changed
        const currentBody = filterEmptyPairs(formData.body);
        const savedBody = filterEmptyPairs(savedData.body);
        if (JSON.stringify(currentBody) !== JSON.stringify(savedBody)) {
          const response = await updateResourceBody({
            id: resourceId,
            body: currentBody,
          });
          if (response?.error) {
            errors.push(response.error.message || "Failed to save body");
          }
        }
      }

      if (formData.resourceKind === "sql") {
        if (
          !isDatabaseConfigEqual(
            formData.databaseConfig,
            savedData.databaseConfig
          )
        ) {
          const parsedPort = Number.parseInt(formData.databaseConfig.port, 10);

          if (Number.isNaN(parsedPort)) {
            errors.push("Database port must be a number");
          } else {
            const response = await updateResourceDatabaseConfig({
              id: resourceId,
              databaseName: formData.databaseConfig.databaseName.trim(),
              databaseHost: formData.databaseConfig.host.trim(),
              databasePort: parsedPort,
              databaseEngine: formData.databaseConfig.engine,
              databaseAuthScheme: formData.databaseConfig.authScheme,
              databaseUsername: formData.databaseConfig.username.trim(),
              databasePassword:
                formData.databaseConfig.password.trim() || undefined,
              useSsl: formData.databaseConfig.useSsl,
              enableSshTunnel: formData.databaseConfig.enableSshTunnel,
              connectionOptions: filterEmptyPairs(
                formData.databaseConfig.connectionOptions
              ),
            });

            if (response?.error) {
              errors.push(
                response.error.message || "Failed to save database config"
              );
            }
          }
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
  }, [resourceId, formData, hasUnsavedChanges, onUpdate]);

  return {
    formData,
    saveState,
    hasUnsavedChanges,
    updateFormData,
    saveConfig,
    discardChanges,
    setFormData,
    resetFormData,
  };
}

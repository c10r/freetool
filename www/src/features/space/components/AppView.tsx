import { Check, Edit, Eye, Loader2, Plus, X } from "lucide-react";
import { useEffect, useState } from "react";
import { updateAppName } from "@/api/api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { useHasPermission } from "@/hooks/usePermissions";
import { useAppForm } from "../hooks/useAppForm";
import { useAppInputs } from "../hooks/useAppInputs";
import type {
  AppNode,
  EndpointMethod,
  KeyValuePair,
  SpaceMainProps,
} from "../types";
import AppConfigForm from "./AppConfigForm";
import InputFieldEditor from "./InputFieldEditor";
import ResourceSelector from "./ResourceSelector";

export default function AppView({
  app,
  updateNode,
  endpoints,
  spaceId,
}: SpaceMainProps & { app: AppNode }) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(app.name);
  const [nameUpdating, setNameUpdating] = useState(false);
  const [nameSaved, setNameSaved] = useState(false);
  const [nameError, setNameError] = useState(false);
  const [nameErrorMessage, setNameErrorMessage] = useState("");

  // Permission checks
  const canEditApp = useHasPermission(spaceId, "edit_app");

  // App form hook for autosave functionality
  const {
    formData: appFormData,
    fieldStates: appFieldStates,
    updateFormData: updateAppFormData,
    updateKeyValueField,
    updateUrlPathField,
    updateHttpMethodField,
    resetFieldStates,
    setFormData: setAppFormData,
  } = useAppForm(
    {
      httpMethod: app.httpMethod,
      urlPath: app.urlPath || "",
      urlParameters: app.urlParameters || [],
      headers: app.headers || [],
      body: app.body || [],
    },
    app.id,
    (updatedData) => {
      // Update the app node when autosave occurs (only if user has edit permission)
      if (canEditApp) {
        updateNode({
          ...app,
          httpMethod: updatedData.httpMethod,
          urlPath: updatedData.urlPath,
          urlParameters: updatedData.urlParameters,
          headers: updatedData.headers,
          body: updatedData.body,
        });
      }
    }
  );

  // App inputs hook for field autosave functionality
  const {
    fields,
    inputsState,
    addField,
    resetState: resetInputsState,
    setFields,
    triggerSave,
  } = useAppInputs(app.fields, app.id, (updatedFields) => {
    // Update the app node when autosave occurs (only if user has edit permission)
    if (canEditApp) {
      updateNode({
        ...app,
        fields: updatedFields,
      });
    }
  });

  // Update form data when app changes
  useEffect(() => {
    setName(app.name);
    setEditing(false);
    // Reset states when app changes
    setNameUpdating(false);
    setNameSaved(false);
    setNameError(false);
    setNameErrorMessage("");

    // Update app form data when app changes
    setAppFormData({
      httpMethod: app.httpMethod,
      urlPath: app.urlPath || "",
      urlParameters: app.urlParameters || [],
      headers: app.headers || [],
      body: app.body || [],
    });
    resetFieldStates();

    // Update app inputs when app changes
    setFields(app.fields);
    resetInputsState();
  }, [
    app.name,
    app.httpMethod,
    app.urlPath,
    app.urlParameters,
    app.headers,
    app.body,
    app.fields,
    setAppFormData,
    resetFieldStates,
    setFields,
    resetInputsState,
  ]);

  const updateHttpMethod = (httpMethod: EndpointMethod) => {
    updateAppFormData("httpMethod", httpMethod);
    // Don't update the app node immediately - wait for autosave
  };

  const updateUrlParameters = (urlParameters: KeyValuePair[]) => {
    updateAppFormData("urlParameters", urlParameters);
    // Don't update the app node immediately - wait for autosave
  };

  const updateHeaders = (headers: KeyValuePair[]) => {
    updateAppFormData("headers", headers);
    // Don't update the app node immediately - wait for autosave
  };

  const updateBody = (body: KeyValuePair[]) => {
    updateAppFormData("body", body);
    // Don't update the app node immediately - wait for autosave
  };

  const updateUrlPath = (urlPath: string) => {
    updateAppFormData("urlPath", urlPath);
    // Don't update the app node immediately - wait for autosave
  };

  const _selectedEndpoint = app.endpointId
    ? endpoints[app.endpointId]
    : undefined;

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          {editing ? (
            <div className="flex items-center gap-2">
              <div className="relative">
                <Input
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-64 pr-10"
                  disabled={nameUpdating || !canEditApp}
                />
                {nameUpdating && (
                  <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900" />
                  </div>
                )}
                {nameSaved && !nameUpdating && !nameError && (
                  <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                    <Check className="h-4 w-4 text-green-500" />
                  </div>
                )}
                {nameError && !nameUpdating && (
                  <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                    <X className="h-4 w-4 text-red-500" />
                  </div>
                )}
              </div>
              <Button
                onClick={async () => {
                  if (!name.trim() || name.trim() === app.name) {
                    setEditing(false);
                    return;
                  }

                  setNameUpdating(true);
                  setNameError(false);
                  setNameErrorMessage("");

                  try {
                    const response = await updateAppName(app.id, name.trim());

                    if (response.error) {
                      const errorMessage =
                        response.error.message || "Failed to save name";
                      setNameError(true);
                      setNameErrorMessage(errorMessage as string);
                      setName(app.name); // Reset to original name

                      // Hide error after 2 seconds
                      setTimeout(() => {
                        setNameError(false);
                        setNameErrorMessage("");
                      }, 2_000);

                      return;
                    }

                    // Update local state on success
                    updateNode({ ...app, name: name.trim() });
                    setEditing(false);

                    // Show success indicator
                    setNameSaved(true);
                    setTimeout(() => {
                      setNameSaved(false);
                    }, 2_000);
                  } catch (_error) {
                    setNameError(true);
                    setNameErrorMessage("Network error occurred");
                    setName(app.name); // Reset to original name

                    // Hide error after 2 seconds
                    setTimeout(() => {
                      setNameError(false);
                      setNameErrorMessage("");
                    }, 2_000);
                  } finally {
                    setNameUpdating(false);
                  }
                }}
                disabled={nameUpdating}
              >
                {nameUpdating ? "Saving..." : "Save"}
              </Button>
            </div>
          ) : (
            <>
              <h2 className="text-2xl font-semibold">{app.name}</h2>
              {!canEditApp && (
                <Badge variant="secondary" className="ml-2">
                  <Eye className="w-3 h-3 mr-1" />
                  View Only
                </Badge>
              )}
            </>
          )}
          {canEditApp && (
            <Button
              variant="secondary"
              size="icon"
              onClick={() => setEditing((v) => !v)}
              aria-label="Rename app"
            >
              <Edit size={16} />
            </Button>
          )}
        </div>
        <div className="flex gap-2 items-center">
          <ResourceSelector
            value={app.resourceId}
            onValueChange={(resourceId) => updateNode({ ...app, resourceId })}
            className="w-64"
            disabled={!canEditApp}
          />
          {canEditApp && (
            <Button onClick={addField}>
              <Plus className="mr-2 h-4 w-4" /> Add Field
            </Button>
          )}
        </div>
        {nameErrorMessage && (
          <div className="text-red-500 text-sm bg-red-50 p-2 rounded mt-2">
            {nameErrorMessage}
          </div>
        )}
      </header>

      {!canEditApp && (
        <div className="bg-muted/50 border border-muted rounded-lg p-4">
          <p className="text-sm text-muted-foreground">
            <Eye className="w-4 h-4 inline mr-2" />
            You have view-only access to this app. To make changes, contact your
            team admin to request edit_app permission.
          </p>
        </div>
      )}

      <Separator />

      {/* Fields section header with save status indicators */}
      {fields.length > 0 && (
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-muted-foreground">
            Fields
          </span>
          {inputsState.updating && (
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
          )}
          {inputsState.saved && !inputsState.updating && !inputsState.error && (
            <Check className="h-4 w-4 text-green-500" />
          )}
          {inputsState.error && !inputsState.updating && (
            <div className="flex items-center gap-1">
              <X className="h-4 w-4 text-red-500" />
              {inputsState.errorMessage && (
                <span className="text-xs text-red-500">
                  {inputsState.errorMessage}
                </span>
              )}
            </div>
          )}
        </div>
      )}

      <InputFieldEditor
        fields={fields}
        onChange={(updatedFields) => {
          setFields(updatedFields);
          triggerSave(updatedFields);
        }}
        disabled={!canEditApp || inputsState.updating}
        showAddButton={false}
      />

      <Card>
        <CardContent className="py-4">
          <AppConfigForm
            resourceId={app.resourceId}
            httpMethod={appFormData.httpMethod}
            urlPath={appFormData.urlPath}
            queryParameters={appFormData.urlParameters}
            headers={appFormData.headers}
            body={appFormData.body}
            onResourceChange={(resourceId) =>
              updateNode({ ...app, resourceId })
            }
            onHttpMethodChange={updateHttpMethod}
            onUrlPathChange={updateUrlPath}
            onQueryParametersChange={updateUrlParameters}
            onHeadersChange={updateHeaders}
            onBodyChange={updateBody}
            showResourceSelector={false}
            mode="edit"
            fieldStates={appFieldStates}
            onKeyValueFieldBlur={(field, items) => {
              updateKeyValueField(field, items);
            }}
            onUrlPathFieldBlur={(value) => {
              updateUrlPathField(value);
            }}
            onHttpMethodFieldBlur={(value) => {
              updateHttpMethodField(value);
            }}
            disabled={!canEditApp}
            inputs={fields.map((f) => ({
              title: f.label,
              required: f.required,
            }))}
          />
        </CardContent>
      </Card>
    </section>
  );
}

import { Check, Edit, Eye, Loader2, Plus, Save, X } from "lucide-react";
import { useEffect, useState } from "react";
import { updateAppDescription, updateAppName } from "@/api/api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
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

  // Description editing state
  const [descriptionEditing, setDescriptionEditing] = useState(false);
  const [description, setDescription] = useState(app.description || "");
  const [descriptionUpdating, setDescriptionUpdating] = useState(false);
  const [descriptionSaved, setDescriptionSaved] = useState(false);
  const [descriptionError, setDescriptionError] = useState(false);
  const [descriptionErrorMessage, setDescriptionErrorMessage] = useState("");

  // Permission checks
  const canEditApp = useHasPermission(spaceId, "edit_app");

  // App form hook for config fields (manual save)
  const {
    formData: appFormData,
    saveState: configSaveState,
    hasUnsavedChanges: hasUnsavedConfigChanges,
    updateFormData: updateAppFormData,
    saveConfig,
    setFormData: setAppFormData,
  } = useAppForm(
    {
      httpMethod: app.httpMethod,
      urlPath: app.urlPath || "",
      urlParameters: app.urlParameters || [],
      headers: app.headers || [],
      body: app.body || [],
      useDynamicJsonBody: app.useDynamicJsonBody ?? false,
      sqlConfig: app.sqlConfig,
    },
    app.id,
    (updatedData) => {
      // Update the app node when save occurs (only if user has edit permission)
      if (canEditApp) {
        updateNode({
          ...app,
          httpMethod: updatedData.httpMethod,
          urlPath: updatedData.urlPath,
          urlParameters: updatedData.urlParameters,
          headers: updatedData.headers,
          body: updatedData.body,
          useDynamicJsonBody: updatedData.useDynamicJsonBody,
          sqlConfig: updatedData.sqlConfig,
        });
      }
    }
  );

  // App inputs hook for field management (manual save)
  const {
    fields,
    inputsState,
    hasUnsavedChanges: hasUnsavedFieldChanges,
    addField,
    resetState: resetInputsState,
    setFields,
    saveInputs,
  } = useAppInputs(app.fields, app.id, (updatedFields) => {
    // Update the app node when save occurs (only if user has edit permission)
    if (canEditApp) {
      updateNode({
        ...app,
        fields: updatedFields,
      });
    }
  });

  const normalizeDescription = (value: string) => value.trim();
  const currentDescription = normalizeDescription(app.description || "");
  const draftDescription = normalizeDescription(description);
  const hasUnsavedDescriptionChanges = draftDescription !== currentDescription;

  // Combined unsaved changes state
  const hasUnsavedChanges =
    hasUnsavedConfigChanges ||
    hasUnsavedFieldChanges ||
    hasUnsavedDescriptionChanges;
  const isSaving =
    configSaveState.saving || inputsState.updating || descriptionUpdating;

  const saveDescription = async () => {
    if (!hasUnsavedDescriptionChanges) {
      return { success: true };
    }

    setDescriptionUpdating(true);
    setDescriptionError(false);
    setDescriptionErrorMessage("");

    try {
      const response = await updateAppDescription(
        app.id,
        draftDescription || null
      );

      if (response.error) {
        const errorMessage =
          response.error.message || "Failed to save description";
        setDescriptionError(true);
        setDescriptionErrorMessage(errorMessage as string);
        setDescription(app.description || "");

        setTimeout(() => {
          setDescriptionError(false);
          setDescriptionErrorMessage("");
        }, 2_000);

        return { success: false };
      }

      updateNode({
        ...app,
        description: draftDescription || undefined,
      });

      setDescriptionSaved(true);
      setTimeout(() => {
        setDescriptionSaved(false);
      }, 2_000);

      return { success: true };
    } catch (_error) {
      setDescriptionError(true);
      setDescriptionErrorMessage("Network error occurred");
      setDescription(app.description || "");

      setTimeout(() => {
        setDescriptionError(false);
        setDescriptionErrorMessage("");
      }, 2_000);

      return { success: false };
    } finally {
      setDescriptionUpdating(false);
    }
  };

  // Handle save all changes
  const handleSaveAll = async () => {
    const results = await Promise.all([
      saveInputs(),
      saveConfig(),
      saveDescription(),
    ]);
    const hasError = results.some((r) => !r.success);
    return !hasError;
  };

  // Update form data when app changes
  useEffect(() => {
    setName(app.name);
    setEditing(false);
    // Reset states when app changes
    setNameUpdating(false);
    setNameSaved(false);
    setNameError(false);
    setNameErrorMessage("");

    // Reset description state when app changes
    setDescription(app.description || "");
    setDescriptionEditing(false);
    setDescriptionUpdating(false);
    setDescriptionSaved(false);
    setDescriptionError(false);
    setDescriptionErrorMessage("");

    // Update app form data when app changes
    setAppFormData({
      httpMethod: app.httpMethod,
      urlPath: app.urlPath || "",
      urlParameters: app.urlParameters || [],
      headers: app.headers || [],
      body: app.body || [],
      useDynamicJsonBody: app.useDynamicJsonBody ?? false,
      sqlConfig: app.sqlConfig,
    });

    // Update app inputs when app changes
    setFields(app.fields);
    resetInputsState();
  }, [app, setAppFormData, setFields, resetInputsState]);

  const updateHttpMethod = (httpMethod: EndpointMethod) => {
    updateAppFormData("httpMethod", httpMethod);
  };

  const updateUrlParameters = (urlParameters: KeyValuePair[]) => {
    updateAppFormData("urlParameters", urlParameters);
  };

  const updateHeaders = (headers: KeyValuePair[]) => {
    updateAppFormData("headers", headers);
  };

  const updateBody = (body: KeyValuePair[]) => {
    updateAppFormData("body", body);
  };

  const updateUrlPath = (urlPath: string) => {
    updateAppFormData("urlPath", urlPath);
  };

  const updateUseDynamicJsonBody = (useDynamicJsonBody: boolean) => {
    updateAppFormData("useDynamicJsonBody", useDynamicJsonBody);
  };

  const updateSqlConfig = (sqlConfig: AppNode["sqlConfig"]) => {
    if (sqlConfig) {
      updateAppFormData("sqlConfig", sqlConfig);
    }
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
          {/* Save Changes Button */}
          {canEditApp && hasUnsavedChanges && (
            <Button
              onClick={handleSaveAll}
              disabled={isSaving}
              className="gap-2"
            >
              {isSaving ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
                  Save Changes
                </>
              )}
            </Button>
          )}
          {/* Show saved indicator briefly after save */}
          {canEditApp &&
            !hasUnsavedChanges &&
            (configSaveState.saved ||
              inputsState.saved ||
              descriptionSaved) && (
              <div className="flex items-center gap-1 text-green-600 text-sm">
                <Check className="h-4 w-4" />
                Saved
              </div>
            )}
          {/* Show error if save failed */}
          {(configSaveState.error || inputsState.error || descriptionError) && (
            <div className="flex items-center gap-1 text-red-600 text-sm">
              <X className="h-4 w-4" />
              {configSaveState.errorMessage ||
                inputsState.errorMessage ||
                descriptionErrorMessage}
            </div>
          )}
          <div className="flex flex-col gap-1">
            <Label htmlFor="app-resource-select">Resource *</Label>
            <ResourceSelector
              id="app-resource-select"
              spaceId={spaceId}
              value={app.resourceId}
              onValueChange={(resourceId) => updateNode({ ...app, resourceId })}
              className="w-64"
              readOnly
              readOnlyTooltip="For safety, you can't change an app's resource after it's created. Create a new app if you need to use a different resource."
              required
            />
          </div>
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

      {/* Description editing */}
      <div className="flex items-center gap-2 -mt-2">
        {descriptionEditing ? (
          <div className="flex items-center gap-2 flex-1">
            <div className="relative flex-1 max-w-md">
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Add a description..."
                className="pr-10"
                disabled={descriptionUpdating || !canEditApp}
              />
              {descriptionUpdating && (
                <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900" />
                </div>
              )}
              {descriptionSaved &&
                !descriptionUpdating &&
                !descriptionError && (
                  <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                    <Check className="h-4 w-4 text-green-500" />
                  </div>
                )}
              {descriptionError && !descriptionUpdating && (
                <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                  <X className="h-4 w-4 text-red-500" />
                </div>
              )}
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setDescription(app.description || "");
                setDescriptionEditing(false);
              }}
              disabled={descriptionUpdating}
            >
              Cancel
            </Button>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <p className="text-sm text-muted-foreground">
              {app.description || (canEditApp ? "No description" : "")}
            </p>
            {canEditApp && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setDescriptionEditing(true)}
                className="h-6 px-2"
              >
                <Edit size={12} className="mr-1" />
                {app.description ? "Edit" : "Add description"}
              </Button>
            )}
          </div>
        )}
      </div>

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

      {/* Fields section header */}
      {fields.length > 0 && (
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-muted-foreground">
            Fields
          </span>
          {hasUnsavedFieldChanges && (
            <Badge variant="outline" className="text-xs">
              Unsaved
            </Badge>
          )}
        </div>
      )}

      <InputFieldEditor
        fields={fields}
        onChange={(updatedFields) => {
          setFields(updatedFields);
        }}
        disabled={!canEditApp}
        showAddButton={false}
      />

      <Card>
        <CardContent className="py-4">
          <AppConfigForm
            spaceId={spaceId}
            resourceId={app.resourceId}
            httpMethod={appFormData.httpMethod}
            urlPath={appFormData.urlPath}
            queryParameters={appFormData.urlParameters}
            headers={appFormData.headers}
            body={appFormData.body}
            useDynamicJsonBody={appFormData.useDynamicJsonBody}
            sqlConfig={appFormData.sqlConfig}
            onResourceChange={(resourceId) =>
              updateNode({ ...app, resourceId })
            }
            onHttpMethodChange={updateHttpMethod}
            onUrlPathChange={updateUrlPath}
            onQueryParametersChange={updateUrlParameters}
            onHeadersChange={updateHeaders}
            onBodyChange={updateBody}
            onUseDynamicJsonBodyChange={updateUseDynamicJsonBody}
            onSqlConfigChange={updateSqlConfig}
            showResourceSelector={false}
            mode="edit"
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

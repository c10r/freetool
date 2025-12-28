import { useState, useEffect } from "react";
import { Plus, Edit, Trash2, Check, X, Eye } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import {
  AppNode,
  AppField,
  FieldType,
  WorkspaceMainProps,
  KeyValuePair,
} from "../types";
import AppFormRenderer from "./AppFormRenderer";
import AppConfigForm from "./AppConfigForm";
import ResourceSelector from "./ResourceSelector";
import { Switch } from "@/components/ui/switch";
import { updateAppName } from "@/api/api";
import { useAppForm } from "../hooks/useAppForm";
import { useHasPermission } from "@/hooks/usePermissions";
import { Badge } from "@/components/ui/badge";

export default function AppView({
  app,
  updateNode,
  endpoints,
  workspaceId,
}: WorkspaceMainProps & { app: AppNode }) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(app.name);
  const [nameUpdating, setNameUpdating] = useState(false);
  const [nameSaved, setNameSaved] = useState(false);
  const [nameError, setNameError] = useState(false);
  const [nameErrorMessage, setNameErrorMessage] = useState("");

  // Permission checks
  const canEditApp = useHasPermission(workspaceId, "edit_app");

  // App form hook for autosave functionality
  const {
    formData: appFormData,
    fieldStates: appFieldStates,
    updateFormData: updateAppFormData,
    updateKeyValueField,
    updateUrlPathField,
    resetFieldStates,
    setFormData: setAppFormData,
  } = useAppForm(
    {
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
          urlPath: updatedData.urlPath,
          urlParameters: updatedData.urlParameters,
          headers: updatedData.headers,
          body: updatedData.body,
        });
      }
    },
  );

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
      urlPath: app.urlPath || "",
      urlParameters: app.urlParameters || [],
      headers: app.headers || [],
      body: app.body || [],
    });
    resetFieldStates();
  }, [app.id, app.name, app.urlPath, app.urlParameters, app.headers, app.body, setAppFormData, resetFieldStates]);

  const updateField = (id: string, patch: Partial<AppField>) => {
    updateNode({
      ...app,
      fields: app.fields.map((f) => (f.id === id ? { ...f, ...patch } : f)),
    });
  };
  const deleteField = (id: string) => {
    updateNode({ ...app, fields: app.fields.filter((f) => f.id !== id) });
  };
  const addField = () => {
    const id = crypto.randomUUID();
    const nf: AppField = {
      id,
      label: `Field ${app.fields.length + 1}`,
      type: "text",
    };
    updateNode({ ...app, fields: [...app.fields, nf] });
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

  const selectedEndpoint = app.endpointId
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
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
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
                  } catch (error) {
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
      {app.fields.map((f) => (
        <Card key={f.id}>
          <CardContent className="py-4 grid grid-cols-1 md:grid-cols-12 gap-3 items-center">
            <div className="md:col-span-4">
              <Input
                value={f.label}
                onChange={(e) => updateField(f.id, { label: e.target.value })}
                aria-label="Field label"
                disabled={!canEditApp}
              />
            </div>
            <div className="md:col-span-3">
              <Select
                value={f.type}
                onValueChange={(v: FieldType) => updateField(f.id, { type: v })}
                disabled={!canEditApp}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="text">Text</SelectItem>
                  <SelectItem value="email">Email</SelectItem>
                  <SelectItem value="date">Date</SelectItem>
                  <SelectItem value="integer">Integer</SelectItem>
                  <SelectItem value="boolean">Boolean</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="md:col-span-3 flex items-center gap-2">
              <Switch
                checked={!!f.required}
                onCheckedChange={(v) => updateField(f.id, { required: v })}
                disabled={!canEditApp}
              />
              <span className="text-sm text-muted-foreground">Required</span>
            </div>
            <div className="md:col-span-2 flex justify-end">
              {canEditApp && (
                <Button
                  variant="secondary"
                  size="icon"
                  onClick={() => deleteField(f.id)}
                  aria-label="Delete field"
                >
                  <Trash2 size={16} />
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      ))}

      <Card>
        <CardContent className="py-4">
          <AppConfigForm
            resourceId={app.resourceId}
            urlPath={appFormData.urlPath}
            queryParameters={appFormData.urlParameters}
            headers={appFormData.headers}
            body={appFormData.body}
            onResourceChange={(resourceId) =>
              updateNode({ ...app, resourceId })
            }
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
            disabled={!canEditApp}
          />
        </CardContent>
      </Card>
    </section>
  );
}

import { useState, useEffect } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Plus, Edit, Trash2, Check, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { AppNode, AppField, FieldType, WorkspaceMainProps } from "../types";
import AppFormRenderer from "./AppFormRenderer";
import HttpMethodBadge from "./HttpMethodBadge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { updateAppName, getResources } from "@/api/api";

export default function AppView({
  app,
  updateNode,
  endpoints,
}: WorkspaceMainProps & { app: AppNode }) {
  const [tab, setTab] = useState("build");
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(app.name);
  const [nameUpdating, setNameUpdating] = useState(false);
  const [nameSaved, setNameSaved] = useState(false);
  const [nameError, setNameError] = useState(false);
  const [nameErrorMessage, setNameErrorMessage] = useState("");

  // Resource picker state
  const [resources, setResources] = useState<
    Array<{ id: string; name: string; httpMethod: string }>
  >([]);
  const [loadingResources, setLoadingResources] = useState(false);
  const [resourcesError, setResourcesError] = useState<string | null>(null);

  // Update name when app changes
  useEffect(() => {
    setName(app.name);
    setEditing(false);
    // Reset states when app changes
    setNameUpdating(false);
    setNameSaved(false);
    setNameError(false);
    setNameErrorMessage("");
  }, [app.id, app.name]);

  // Fetch resources on component mount
  useEffect(() => {
    const fetchResources = async () => {
      setLoadingResources(true);
      setResourcesError(null);
      try {
        const response = await getResources();
        if (response.error) {
          setResourcesError("Failed to load resources");
          setResources([]);
        } else if (response.data?.items) {
          const resourceList = response.data.items.map((item) => ({
            id: item.id!,
            name: item.name,
            httpMethod: item.httpMethod,
          }));
          setResources(resourceList);
        } else {
          // No items in response, set empty array
          setResources([]);
        }
      } catch (error) {
        setResourcesError("Failed to load resources");
        setResources([]);
      } finally {
        setLoadingResources(false);
      }
    };

    fetchResources();
  }, []);

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

  const endpointList = Object.values(endpoints);
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
                  disabled={nameUpdating}
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
            <h2 className="text-2xl font-semibold">{app.name}</h2>
          )}
          <Button
            variant="secondary"
            size="icon"
            onClick={() => setEditing((v) => !v)}
            aria-label="Rename app"
          >
            <Edit size={16} />
          </Button>
        </div>
        <div className="flex gap-2 items-center">
          <div className="w-64">
            <Select
              value={app.resourceId || ""}
              onValueChange={(v) =>
                updateNode({ ...app, resourceId: v || undefined })
              }
              disabled={loadingResources}
            >
              <SelectTrigger aria-label="Select Resource">
                <SelectValue
                  placeholder={
                    loadingResources
                      ? "Loading resources..."
                      : "Select Resource"
                  }
                />
              </SelectTrigger>
              <SelectContent>
                {resources.length > 0 ? (
                  resources.map((resource) => (
                    <SelectItem key={resource.id} value={resource.id}>
                      <div className="flex items-center gap-2">
                        <span>{resource.name}</span>
                        <HttpMethodBadge method={resource.httpMethod as any} />
                      </div>
                    </SelectItem>
                  ))
                ) : (
                  <div className="px-2 py-1.5 text-sm text-muted-foreground">
                    {loadingResources
                      ? "Loading resources..."
                      : resourcesError ||
                        "No resources available - Create one first"}
                  </div>
                )}
              </SelectContent>
            </Select>
          </div>
          <Button onClick={addField}>
            <Plus className="mr-2 h-4 w-4" /> Add Field
          </Button>
        </div>
        {nameErrorMessage && (
          <div className="text-red-500 text-sm bg-red-50 p-2 rounded mt-2">
            {nameErrorMessage}
          </div>
        )}
      </header>
      <Separator />
      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="build">Build</TabsTrigger>
          <TabsTrigger value="fill">Fill</TabsTrigger>
        </TabsList>
        <TabsContent value="build" className="space-y-6">
          {app.fields.length === 0 && (
            <Card>
              <CardContent className="py-10 text-center text-muted-foreground">
                No fields. Add one to start.
              </CardContent>
            </Card>
          )}
          {app.fields.map((f) => (
            <Card key={f.id}>
              <CardContent className="py-4 grid grid-cols-1 md:grid-cols-12 gap-3 items-center">
                <div className="md:col-span-4">
                  <Input
                    value={f.label}
                    onChange={(e) =>
                      updateField(f.id, { label: e.target.value })
                    }
                    aria-label="Field label"
                  />
                </div>
                <div className="md:col-span-3">
                  <Select
                    value={f.type}
                    onValueChange={(v: FieldType) =>
                      updateField(f.id, { type: v })
                    }
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
                  />
                  <span className="text-sm text-muted-foreground">
                    Required
                  </span>
                </div>
                <div className="md:col-span-2 flex justify-end">
                  <Button
                    variant="secondary"
                    size="icon"
                    onClick={() => deleteField(f.id)}
                    aria-label="Delete field"
                  >
                    <Trash2 size={16} />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </TabsContent>
        <TabsContent value="fill">
          <AppFormRenderer app={app} endpoint={selectedEndpoint} />
        </TabsContent>
      </Tabs>
    </section>
  );
}

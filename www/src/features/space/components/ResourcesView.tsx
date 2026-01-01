import { ChevronLeft, Edit, Plus, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import {
  createResource,
  deleteResource,
  getApps,
  getResources,
} from "@/api/api";
import { PermissionButton } from "@/components/PermissionButton";
import { PermissionGate } from "@/components/PermissionGate";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useHasPermission } from "@/hooks/usePermissions";
import { useResourceForm } from "../hooks/useResourceForm";
import type { KeyValuePair } from "../types";
import HttpMethodBadge from "./HttpMethodBadge";
import ResourceForm, { type ResourceFormData } from "./ResourceForm";

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

interface Resource {
  id: string;
  name: string;
  description: string;
  baseUrl: string;
  httpMethod: HttpMethod;
  urlParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
}

const initialFormData: ResourceFormData = {
  name: "",
  description: "",
  baseUrl: "",
  httpMethod: "GET",
  urlParameters: [],
  headers: [],
  body: [],
};

interface ResourcesViewProps {
  spaceId: string;
  spaceName?: string;
  onBackClick?: () => void;
}

export default function ResourcesView({
  spaceId,
  spaceName,
  onBackClick,
}: ResourcesViewProps) {
  const [editingResource, setEditingResource] = useState<Resource | null>(null);
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [apps, setApps] = useState<
    Array<{ id: string; name: string; resourceId?: string }>
  >([]);
  const [deletingResourceId, setDeletingResourceId] = useState<string | null>(
    null
  );

  // Permission checks
  const canEditResource = useHasPermission(spaceId, "edit_resource");

  // Create form state
  const { formData: createFormData, setFormData: setCreateFormData } =
    useResourceForm(initialFormData);

  // Edit form state
  const {
    formData: editFormData,
    fieldStates: editFieldStates,
    updateTextField,
    updateKeyValueField,
    resetFieldStates,
    setFormData: setEditFormData,
  } = useResourceForm(initialFormData, editingResource?.id, (updatedData) => {
    // Update the resources list when edit form data changes
    setResources((prev) =>
      prev.map((r) =>
        r.id === editingResource?.id ? { ...r, ...updatedData } : r
      )
    );
    // Update the editing resource
    setEditingResource((prev) => (prev ? { ...prev, ...updatedData } : null));
  });

  const fetchApps = useCallback(async () => {
    try {
      const response = await getApps();
      if (response.data?.items) {
        const mappedApps = response.data.items.map((item) => ({
          id: item.id ?? "",
          name: item.name,
          resourceId: item.resourceId,
        }));
        setApps(mappedApps);
      }
    } catch (_err) {
      // Silently handle fetch errors - apps list is for validation purposes only
    }
  }, []);

  const fetchResources = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getResources();
      if (response.data?.items) {
        const mappedItems = response.data?.items.map((item) => {
          return {
            id: item.id ?? "",
            name: item.name,
            description: item.description,
            baseUrl: item.baseUrl,
            httpMethod: item.httpMethod,
            urlParameters: item.urlParameters || [],
            headers: item.headers || [],
            body: item.body || [],
          } as Resource;
        });
        setResources(mappedItems);
      }
      // Also fetch apps to check resource usage
      await fetchApps();
    } catch (_err) {
      setError("Failed to load resources");
    } finally {
      setLoading(false);
    }
  }, [fetchApps]);

  const getAppsUsingResource = (resourceId: string) => {
    return apps.filter((app) => app.resourceId === resourceId);
  };

  const canDeleteResource = (resourceId: string) => {
    return getAppsUsingResource(resourceId).length === 0;
  };

  const handleDeleteResource = async (resourceId: string) => {
    if (!canDeleteResource(resourceId)) {
      return;
    }

    try {
      setDeletingResourceId(resourceId);
      await deleteResource(resourceId);
      await fetchResources();
    } catch (_err) {
      setError("Failed to delete resource. Please try again.");
    } finally {
      setDeletingResourceId(null);
    }
  };

  useEffect(() => {
    fetchResources();
  }, [fetchResources]);

  const handleCreateResource = async () => {
    if (!(createFormData.name.trim() && createFormData.baseUrl.trim())) {
      setCreateError("Name and Base URL are required");
      return;
    }

    try {
      setCreating(true);
      setCreateError(null);

      await createResource({
        spaceId,
        name: createFormData.name.trim(),
        description: createFormData.description.trim(),
        baseUrl: createFormData.baseUrl.trim(),
        httpMethod: createFormData.httpMethod,
        urlParameters: createFormData.urlParameters,
        headers: createFormData.headers,
        body: createFormData.body,
      });

      setCreateFormData(initialFormData);
      setShowCreateForm(false);
      await fetchResources();
    } catch (_err) {
      setCreateError("Failed to create resource. Please try again.");
    } finally {
      setCreating(false);
    }
  };

  const handleCancelCreate = () => {
    setShowCreateForm(false);
    setCreateError(null);
    setCreateFormData(initialFormData);
  };

  const handleEditResource = (resource: Resource) => {
    setEditingResource(resource);
    setEditFormData({
      name: resource.name,
      description: resource.description,
      baseUrl: resource.baseUrl,
      httpMethod: resource.httpMethod,
      urlParameters: resource.urlParameters || [],
      headers: resource.headers || [],
      body: resource.body || [],
    });
  };

  const handleCloseEdit = () => {
    setEditingResource(null);
    setEditFormData(initialFormData);
    resetFieldStates();
  };

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {onBackClick && (
            <Button variant="ghost" size="sm" onClick={onBackClick}>
              <ChevronLeft className="h-4 w-4 mr-1" />
              {spaceName || "Space"}
            </Button>
          )}
          <h2 className="text-2xl font-semibold">Resources</h2>
        </div>
        <div className="flex gap-2">
          <PermissionButton
            spaceId={spaceId}
            permission="create_resource"
            onClick={() => setShowCreateForm(!showCreateForm)}
            variant={showCreateForm ? "secondary" : "default"}
          >
            <Plus className="w-4 h-4 mr-2" />
            {showCreateForm ? "Cancel" : "Create Resource"}
          </PermissionButton>
          <Button onClick={fetchResources} variant="outline">
            Refresh
          </Button>
        </div>
      </header>
      <Separator />

      {showCreateForm && (
        <Card>
          <CardHeader>
            <CardTitle>Create New Resource</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {createError && (
              <div className="text-red-500 text-sm bg-red-50 p-3 rounded">
                {createError}
              </div>
            )}

            <ResourceForm
              data={createFormData}
              onChange={(newData) => {
                setCreateFormData(newData);
              }}
              mode="create"
              disabled={creating}
            />

            <div className="flex gap-2 pt-2">
              <Button
                onClick={handleCreateResource}
                disabled={
                  creating ||
                  !createFormData.name.trim() ||
                  !createFormData.baseUrl.trim()
                }
              >
                {creating ? "Creating..." : "Create Resource"}
              </Button>
              <Button
                onClick={handleCancelCreate}
                variant="outline"
                disabled={creating}
              >
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {loading && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            Loading resources...
          </CardContent>
        </Card>
      )}

      {error && (
        <Card>
          <CardContent className="py-10 text-center text-red-500">
            {error}
          </CardContent>
        </Card>
      )}

      {!(loading || error) && resources.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No resources found.
          </CardContent>
        </Card>
      )}

      {!(loading || error) && resources.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-1 lg:grid-cols-2 xl:grid-cols-3">
          {resources.map((resource) => (
            <Card
              key={resource.id}
              className="transition-transform hover:scale-[1.01]"
            >
              <CardHeader>
                <CardTitle className="flex items-center justify-between">
                  <span>{resource.name}</span>
                  <div className="flex items-center gap-2">
                    <HttpMethodBadge method={resource.httpMethod} />
                    <PermissionGate
                      spaceId={spaceId}
                      permission="edit_resource"
                      showTooltip={true}
                    >
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8"
                        onClick={() => handleEditResource(resource)}
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                    </PermissionGate>
                    <PermissionGate
                      spaceId={spaceId}
                      permission="delete_resource"
                      showTooltip={true}
                    >
                      <TooltipProvider>
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <span>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8"
                                onClick={() =>
                                  handleDeleteResource(resource.id)
                                }
                                disabled={
                                  !canDeleteResource(resource.id) ||
                                  deletingResourceId === resource.id
                                }
                              >
                                <Trash2
                                  className={`h-4 w-4 text-red-500 ${
                                    !canDeleteResource(resource.id) ||
                                    deletingResourceId === resource.id
                                      ? "opacity-50"
                                      : ""
                                  }`}
                                />
                              </Button>
                            </span>
                          </TooltipTrigger>
                          <TooltipContent>
                            {canDeleteResource(resource.id) ? (
                              <p>Delete resource</p>
                            ) : (
                              <p>
                                There are still apps that use this resource.
                                Please delete those first.
                              </p>
                            )}
                          </TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    </PermissionGate>
                  </div>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground mb-2">
                  {resource.description}
                </p>
                <p className="text-xs font-mono bg-gray-50 p-2 rounded">
                  {resource.baseUrl}
                </p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={!!editingResource} onOpenChange={() => handleCloseEdit()}>
        <DialogContent className="max-w-3xl max-h-[80vh] overflow-hidden flex flex-col">
          <DialogHeader>
            <DialogTitle>
              {canEditResource ? "Edit Resource" : "View Resource"}
              {!canEditResource && (
                <p className="text-sm font-normal text-muted-foreground mt-1">
                  You don't have permission to edit this resource. Contact your
                  team admin for access.
                </p>
              )}
            </DialogTitle>
          </DialogHeader>
          {editingResource && (
            <div className="space-y-4 overflow-y-auto flex-1 pr-2">
              <ResourceForm
                data={editFormData}
                onChange={setEditFormData}
                mode="edit"
                fieldStates={editFieldStates}
                onFieldBlur={updateTextField}
                onKeyValueFieldBlur={updateKeyValueField}
                disabled={!canEditResource}
              />
            </div>
          )}
        </DialogContent>
      </Dialog>
    </section>
  );
}

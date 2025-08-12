import { useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { getResources, createResource } from "@/api/api";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import HttpMethodBadge from "./HttpMethodBadge";
import ResourceForm, { ResourceFormData } from "./ResourceForm";
import { useResourceForm } from "../hooks/useResourceForm";
import { Edit } from "lucide-react";
import { KeyValuePair } from "../types";

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

export default function ResourcesView() {
  const [editingResource, setEditingResource] = useState<Resource | null>(null);
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);

  // Create form state
  const {
    formData: createFormData,
    updateFormData: updateCreateFormData,
    setFormData: setCreateFormData,
  } = useResourceForm(initialFormData);

  // Edit form state
  const {
    formData: editFormData,
    fieldStates: editFieldStates,
    updateFormData: updateEditFormData,
    updateTextField,
    updateKeyValueField,
    resetFieldStates,
    setFormData: setEditFormData,
  } = useResourceForm(initialFormData, editingResource?.id, (updatedData) => {
    // Update the resources list when edit form data changes
    setResources((prev) =>
      prev.map((r) =>
        r.id === editingResource?.id ? { ...r, ...updatedData } : r,
      ),
    );
    // Update the editing resource
    setEditingResource((prev) => (prev ? { ...prev, ...updatedData } : null));
  });

  const fetchResources = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getResources();
      if (response.data?.items) {
        const mappedItems = response.data?.items.map((item) => {
          return {
            id: item.id!,
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
    } catch (err) {
      setError("Failed to load resources");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchResources();
  }, []);

  const handleCreateResource = async () => {
    if (!createFormData.name.trim() || !createFormData.baseUrl.trim()) {
      setCreateError("Name and Base URL are required");
      return;
    }

    try {
      setCreating(true);
      setCreateError(null);

      await createResource({
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
    } catch (err) {
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
        <h2 className="text-2xl font-semibold">Resources</h2>
        <div className="flex gap-2">
          <Button
            onClick={() => setShowCreateForm(!showCreateForm)}
            variant={showCreateForm ? "secondary" : "default"}
          >
            {showCreateForm ? "Cancel" : "Create Resource"}
          </Button>
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

      {!loading && !error && resources.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No resources found.
          </CardContent>
        </Card>
      )}

      {!loading && !error && resources.length > 0 && (
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
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => handleEditResource(resource)}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
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
            <DialogTitle>Edit Resource</DialogTitle>
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
              />
            </div>
          )}
        </DialogContent>
      </Dialog>
    </section>
  );
}

import { useState, useEffect, useCallback, useRef } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  getResources,
  createResource,
  updateResourceName,
  updateResourceDescription,
  updateResourceBaseUrl,
  updateResourceUrlParameters,
  updateResourceHeaders,
  updateResourceBody,
} from "@/api/api";
import HttpMethodBadge from "./HttpMethodBadge";
import KeyValueList from "./KeyValueList";
import { Edit, Check, X } from "lucide-react";
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

export default function ResourcesView() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    baseUrl: "",
    httpMethod: "GET" as "GET" | "POST" | "PUT" | "PATCH" | "DELETE",
    urlParameters: [] as KeyValuePair[],
    headers: [] as KeyValuePair[],
    body: [] as KeyValuePair[],
  });
  const [editingResource, setEditingResource] = useState<Resource | null>(null);
  const [editFormData, setEditFormData] = useState({
    name: "",
    description: "",
    baseUrl: "",
    urlParameters: [] as KeyValuePair[],
    headers: [] as KeyValuePair[],
    body: [] as KeyValuePair[],
  });
  const [fieldUpdating, setFieldUpdating] = useState({
    name: false,
    description: false,
    baseUrl: false,
    urlParameters: false,
    headers: false,
    body: false,
  });
  const [fieldSaved, setFieldSaved] = useState({
    name: false,
    description: false,
    baseUrl: false,
    urlParameters: false,
    headers: false,
    body: false,
  });
  const [fieldError, setFieldError] = useState({
    name: false,
    description: false,
    baseUrl: false,
    urlParameters: false,
    headers: false,
    body: false,
  });
  const [fieldErrorMessages, setFieldErrorMessages] = useState({
    name: "",
    description: "",
    baseUrl: "",
    urlParameters: "",
    headers: "",
    body: "",
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
      console.error("Error fetching resources:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchResources();
  }, []);

  const handleCreateResource = async () => {
    if (!formData.name.trim() || !formData.baseUrl.trim()) {
      setCreateError("Name and Base URL are required");
      return;
    }

    try {
      setCreating(true);
      setCreateError(null);

      await createResource({
        name: formData.name.trim(),
        description: formData.description.trim(),
        baseUrl: formData.baseUrl.trim(),
        httpMethod: formData.httpMethod,
        urlParameters: formData.urlParameters,
        headers: formData.headers,
        body: formData.body,
      });

      setFormData({
        name: "",
        description: "",
        baseUrl: "",
        httpMethod: "GET",
        urlParameters: [],
        headers: [],
        body: [],
      });
      setShowCreateForm(false);
      await fetchResources();
    } catch (err) {
      setCreateError("Failed to create resource. Please try again.");
      console.error("Error creating resource:", err);
    } finally {
      setCreating(false);
    }
  };

  const handleCancelCreate = () => {
    setShowCreateForm(false);
    setCreateError(null);
    setFormData({
      name: "",
      description: "",
      baseUrl: "",
      httpMethod: "GET",
      urlParameters: [],
      headers: [],
      body: [],
    });
  };

  const handleEditResource = (resource: Resource) => {
    setEditingResource(resource);
    setEditFormData({
      name: resource.name,
      description: resource.description,
      baseUrl: resource.baseUrl,
      urlParameters: resource.urlParameters || [],
      headers: resource.headers || [],
      body: resource.body || [],
    });
  };

  const handleCloseEdit = () => {
    setEditingResource(null);
    setEditFormData({
      name: "",
      description: "",
      baseUrl: "",
      urlParameters: [],
      headers: [],
      body: [],
    });
    setFieldUpdating({
      name: false,
      description: false,
      baseUrl: false,
      urlParameters: false,
      headers: false,
      body: false,
    });
    setFieldSaved({
      name: false,
      description: false,
      baseUrl: false,
      urlParameters: false,
      headers: false,
      body: false,
    });
    setFieldError({
      name: false,
      description: false,
      baseUrl: false,
      urlParameters: false,
      headers: false,
      body: false,
    });
    setFieldErrorMessages({
      name: "",
      description: "",
      baseUrl: "",
      urlParameters: "",
      headers: "",
      body: "",
    });
  };

  const updateKeyValueField = async (
    field: "urlParameters" | "headers" | "body",
    value: KeyValuePair[],
  ) => {
    if (!editingResource) return;

    // Filter out empty key-value pairs (both key and value must be non-empty)
    const filteredValue = value.filter(
      (pair) => pair.key.trim() !== "" && pair.value.trim() !== "",
    );

    // Don't make network request if value hasn't changed
    const currentValue = (editingResource[field] || []).filter(
      (pair) => pair.key.trim() !== "" && pair.value.trim() !== "",
    );
    if (JSON.stringify(filteredValue) === JSON.stringify(currentValue)) return;

    setFieldUpdating((prev) => ({ ...prev, [field]: true }));

    try {
      let response;
      if (field === "urlParameters") {
        response = await updateResourceUrlParameters({
          id: editingResource.id,
          urlParameters: filteredValue,
        });
      } else if (field === "headers") {
        response = await updateResourceHeaders({
          id: editingResource.id,
          headers: filteredValue,
        });
      } else if (field === "body") {
        response = await updateResourceBody({
          id: editingResource.id,
          body: filteredValue,
        });
      }

      // Check if the response contains an error
      if (response && response.error) {
        console.error(`Error updating ${field}:`, response.error);

        // Extract error message
        const errorMessage = response.error.message || "Failed to save";

        // Show error indicator and message
        setFieldError((prev) => ({ ...prev, [field]: true }));
        setFieldErrorMessages((prev) => ({ ...prev, [field]: errorMessage }));

        // Reset the field value on error
        setEditFormData((prev) => ({
          ...prev,
          [field]: editingResource[field] || [],
        }));

        // Hide error indicator after 2 seconds
        setTimeout(() => {
          setFieldError((prev) => ({ ...prev, [field]: false }));
          setFieldErrorMessages((prev) => ({ ...prev, [field]: "" }));
        }, 2000);

        return;
      }

      // Update local state
      setResources((prev) =>
        prev.map((r) =>
          r.id === editingResource.id ? { ...r, [field]: filteredValue } : r,
        ),
      );

      // Update editing resource
      setEditingResource((prev) =>
        prev ? { ...prev, [field]: filteredValue } : null,
      );

      // Show success indicator only on successful save
      setFieldSaved((prev) => ({ ...prev, [field]: true }));
      setTimeout(() => {
        setFieldSaved((prev) => ({ ...prev, [field]: false }));
      }, 2000);
    } catch (error) {
      console.error(`Error updating ${field}:`, error);

      // Show error indicator
      setFieldError((prev) => ({ ...prev, [field]: true }));
      setFieldErrorMessages((prev) => ({
        ...prev,
        [field]: "Network error occurred",
      }));

      // Reset the field value on error
      setEditFormData((prev) => ({
        ...prev,
        [field]: editingResource[field] || [],
      }));

      // Hide error indicator after 2 seconds
      setTimeout(() => {
        setFieldError((prev) => ({ ...prev, [field]: false }));
        setFieldErrorMessages((prev) => ({ ...prev, [field]: "" }));
      }, 2000);
    } finally {
      setFieldUpdating((prev) => ({ ...prev, [field]: false }));
    }
  };

  const updateField = async (
    field: "name" | "description" | "baseUrl",
    value: string,
  ) => {
    if (!editingResource || !value.trim()) return;

    // Don't make network request if value hasn't changed
    if (value.trim() === editingResource[field]) return;

    setFieldUpdating((prev) => ({ ...prev, [field]: true }));

    try {
      let response;
      if (field === "name") {
        response = await updateResourceName({
          id: editingResource.id,
          newName: value.trim(),
        });
      } else if (field === "description") {
        response = await updateResourceDescription({
          id: editingResource.id,
          newDescription: value.trim(),
        });
      } else if (field === "baseUrl") {
        response = await updateResourceBaseUrl({
          id: editingResource.id,
          baseUrl: value.trim(),
        });
      }

      // Check if the response contains an error
      if (response && response.error) {
        console.error(`Error updating ${field}:`, response.error);

        // Extract error message
        const errorMessage = response.error.message || "Failed to save";

        // Show error indicator and message
        setFieldError((prev) => ({ ...prev, [field]: true }));
        setFieldErrorMessages((prev) => ({ ...prev, [field]: errorMessage }));

        // Reset the field value on error
        setEditFormData((prev) => ({
          ...prev,
          [field]: editingResource[field],
        }));

        // Hide error indicator after 2 seconds
        setTimeout(() => {
          setFieldError((prev) => ({ ...prev, [field]: false }));
          setFieldErrorMessages((prev) => ({ ...prev, [field]: "" }));
        }, 2000);

        return;
      }

      // Update local state
      setResources((prev) =>
        prev.map((r) =>
          r.id === editingResource.id ? { ...r, [field]: value.trim() } : r,
        ),
      );

      // Update editing resource
      setEditingResource((prev) =>
        prev ? { ...prev, [field]: value.trim() } : null,
      );

      // Show success indicator only on successful save
      setFieldSaved((prev) => ({ ...prev, [field]: true }));
      setTimeout(() => {
        setFieldSaved((prev) => ({ ...prev, [field]: false }));
      }, 2000);
    } catch (error) {
      console.error(`Error updating ${field}:`, error);

      // Show error indicator
      setFieldError((prev) => ({ ...prev, [field]: true }));
      setFieldErrorMessages((prev) => ({
        ...prev,
        [field]: "Network error occurred",
      }));

      // Reset the field value on error
      setEditFormData((prev) => ({
        ...prev,
        [field]: editingResource[field],
      }));

      // Hide error indicator after 2 seconds
      setTimeout(() => {
        setFieldError((prev) => ({ ...prev, [field]: false }));
        setFieldErrorMessages((prev) => ({ ...prev, [field]: "" }));
      }, 2000);
    } finally {
      setFieldUpdating((prev) => ({ ...prev, [field]: false }));
    }
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

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name *</Label>
                <Input
                  id="name"
                  placeholder="Enter resource name"
                  value={formData.name}
                  onChange={(e) =>
                    setFormData({ ...formData, name: e.target.value })
                  }
                  disabled={creating}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="httpMethod">HTTP Method</Label>
                <Select
                  value={formData.httpMethod}
                  onValueChange={(value) =>
                    setFormData({
                      ...formData,
                      httpMethod: value as typeof formData.httpMethod,
                    })
                  }
                  disabled={creating}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="GET">GET</SelectItem>
                    <SelectItem value="POST">POST</SelectItem>
                    <SelectItem value="PUT">PUT</SelectItem>
                    <SelectItem value="PATCH">PATCH</SelectItem>
                    <SelectItem value="DELETE">DELETE</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="baseUrl">Base URL *</Label>
              <Input
                id="baseUrl"
                placeholder="https://api.example.com"
                value={formData.baseUrl}
                onChange={(e) =>
                  setFormData({ ...formData, baseUrl: e.target.value })
                }
                disabled={creating}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                placeholder="Enter resource description"
                value={formData.description}
                onChange={(e) =>
                  setFormData({ ...formData, description: e.target.value })
                }
                disabled={creating}
                rows={3}
              />
            </div>

            <div className="space-y-2">
              <Label>Query Parameters</Label>
              <KeyValueList
                items={formData.urlParameters}
                onChange={(items) =>
                  setFormData({ ...formData, urlParameters: items })
                }
                ariaLabel="URL parameters"
              />
            </div>

            <div className="space-y-2">
              <Label>HTTP Headers</Label>
              <KeyValueList
                items={formData.headers}
                onChange={(items) =>
                  setFormData({ ...formData, headers: items })
                }
                ariaLabel="HTTP headers"
              />
            </div>

            <div className="space-y-2">
              <Label>JSON Body</Label>
              <KeyValueList
                items={formData.body}
                onChange={(items) => setFormData({ ...formData, body: items })}
                ariaLabel="JSON body"
              />
            </div>

            <div className="flex gap-2 pt-2">
              <Button
                onClick={handleCreateResource}
                disabled={
                  creating || !formData.name.trim() || !formData.baseUrl.trim()
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
        <DialogContent className="sm:max-w-[500px] max-h-[80vh] overflow-hidden flex flex-col">
          <DialogHeader>
            <DialogTitle>Edit Resource</DialogTitle>
          </DialogHeader>
          {editingResource && (
            <div className="space-y-4 overflow-y-auto flex-1 pr-2">
              <div className="space-y-2">
                <Label htmlFor="edit-name">Name</Label>
                <div className="relative">
                  <Input
                    id="edit-name"
                    value={editFormData.name}
                    onChange={(e) =>
                      setEditFormData({ ...editFormData, name: e.target.value })
                    }
                    onBlur={(e) => {
                      if (e.target.value !== editingResource.name) {
                        updateField("name", e.target.value);
                      }
                    }}
                    disabled={fieldUpdating.name}
                    placeholder="Enter resource name"
                  />
                  {fieldUpdating.name && (
                    <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
                    </div>
                  )}
                  {fieldSaved.name &&
                    !fieldUpdating.name &&
                    !fieldError.name && (
                      <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                        <Check className="h-4 w-4 text-green-500" />
                      </div>
                    )}
                  {fieldError.name && !fieldUpdating.name && (
                    <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                      <X className="h-4 w-4 text-red-500" />
                    </div>
                  )}
                </div>
                {fieldErrorMessages.name && (
                  <div className="text-red-500 text-sm mt-1">
                    {fieldErrorMessages.name}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="edit-description">Description</Label>
                <div className="relative">
                  <Textarea
                    id="edit-description"
                    value={editFormData.description}
                    onChange={(e) =>
                      setEditFormData({
                        ...editFormData,
                        description: e.target.value,
                      })
                    }
                    onBlur={(e) => {
                      if (e.target.value !== editingResource.description) {
                        updateField("description", e.target.value);
                      }
                    }}
                    disabled={fieldUpdating.description}
                    placeholder="Enter resource description"
                    rows={3}
                  />
                  {fieldUpdating.description && (
                    <div className="absolute right-3 top-3">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
                    </div>
                  )}
                  {fieldSaved.description &&
                    !fieldUpdating.description &&
                    !fieldError.description && (
                      <div className="absolute right-3 top-3">
                        <Check className="h-4 w-4 text-green-500" />
                      </div>
                    )}
                  {fieldError.description && !fieldUpdating.description && (
                    <div className="absolute right-3 top-3">
                      <X className="h-4 w-4 text-red-500" />
                    </div>
                  )}
                </div>
                {fieldErrorMessages.description && (
                  <div className="text-red-500 text-sm mt-1">
                    {fieldErrorMessages.description}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="edit-baseUrl">Base URL</Label>
                <div className="relative">
                  <Input
                    id="edit-baseUrl"
                    value={editFormData.baseUrl}
                    onChange={(e) =>
                      setEditFormData({
                        ...editFormData,
                        baseUrl: e.target.value,
                      })
                    }
                    onBlur={(e) => {
                      if (e.target.value !== editingResource.baseUrl) {
                        updateField("baseUrl", e.target.value);
                      }
                    }}
                    disabled={fieldUpdating.baseUrl}
                    placeholder="https://api.example.com"
                  />
                  {fieldUpdating.baseUrl && (
                    <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
                    </div>
                  )}
                  {fieldSaved.baseUrl &&
                    !fieldUpdating.baseUrl &&
                    !fieldError.baseUrl && (
                      <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                        <Check className="h-4 w-4 text-green-500" />
                      </div>
                    )}
                  {fieldError.baseUrl && !fieldUpdating.baseUrl && (
                    <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                      <X className="h-4 w-4 text-red-500" />
                    </div>
                  )}
                </div>
                {fieldErrorMessages.baseUrl && (
                  <div className="text-red-500 text-sm mt-1">
                    {fieldErrorMessages.baseUrl}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label>Query Parameters</Label>
                <div className="relative">
                  <KeyValueList
                    items={editFormData.urlParameters}
                    onChange={(items) => {
                      setEditFormData({
                        ...editFormData,
                        urlParameters: items,
                      });
                    }}
                    onBlur={(items) => {
                      updateKeyValueField("urlParameters", items);
                    }}
                    ariaLabel="URL parameters"
                  />
                  {fieldUpdating.urlParameters && (
                    <div className="absolute right-3 top-3">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
                    </div>
                  )}
                  {fieldSaved.urlParameters &&
                    !fieldUpdating.urlParameters &&
                    !fieldError.urlParameters && (
                      <div className="absolute right-3 top-3">
                        <Check className="h-4 w-4 text-green-500" />
                      </div>
                    )}
                  {fieldError.urlParameters && !fieldUpdating.urlParameters && (
                    <div className="absolute right-3 top-3">
                      <X className="h-4 w-4 text-red-500" />
                    </div>
                  )}
                </div>
                {fieldErrorMessages.urlParameters && (
                  <div className="text-red-500 text-sm mt-1">
                    {fieldErrorMessages.urlParameters}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label>HTTP Headers</Label>
                <div className="relative">
                  <KeyValueList
                    items={editFormData.headers}
                    onChange={(items) => {
                      setEditFormData({ ...editFormData, headers: items });
                    }}
                    onBlur={(items) => {
                      updateKeyValueField("headers", items);
                    }}
                    ariaLabel="HTTP headers"
                  />
                  {fieldUpdating.headers && (
                    <div className="absolute right-3 top-3">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
                    </div>
                  )}
                  {fieldSaved.headers &&
                    !fieldUpdating.headers &&
                    !fieldError.headers && (
                      <div className="absolute right-3 top-3">
                        <Check className="h-4 w-4 text-green-500" />
                      </div>
                    )}
                  {fieldError.headers && !fieldUpdating.headers && (
                    <div className="absolute right-3 top-3">
                      <X className="h-4 w-4 text-red-500" />
                    </div>
                  )}
                </div>
                {fieldErrorMessages.headers && (
                  <div className="text-red-500 text-sm mt-1">
                    {fieldErrorMessages.headers}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label>JSON Body</Label>
                <div className="relative">
                  <KeyValueList
                    items={editFormData.body}
                    onChange={(items) => {
                      setEditFormData({ ...editFormData, body: items });
                    }}
                    onBlur={(items) => {
                      updateKeyValueField("body", items);
                    }}
                    ariaLabel="JSON body"
                  />
                  {fieldUpdating.body && (
                    <div className="absolute right-3 top-3">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
                    </div>
                  )}
                  {fieldSaved.body &&
                    !fieldUpdating.body &&
                    !fieldError.body && (
                      <div className="absolute right-3 top-3">
                        <Check className="h-4 w-4 text-green-500" />
                      </div>
                    )}
                  {fieldError.body && !fieldUpdating.body && (
                    <div className="absolute right-3 top-3">
                      <X className="h-4 w-4 text-red-500" />
                    </div>
                  )}
                </div>
                {fieldErrorMessages.body && (
                  <div className="text-red-500 text-sm mt-1">
                    {fieldErrorMessages.body}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label>HTTP Method</Label>
                <div className="flex items-center gap-2">
                  <HttpMethodBadge method={editingResource.httpMethod} />
                  <span className="text-sm text-muted-foreground">
                    (HTTP method cannot be changed after creation)
                  </span>
                </div>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </section>
  );
}

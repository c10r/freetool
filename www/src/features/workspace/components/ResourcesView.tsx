import { useState, useEffect } from "react";
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
import { getResources, createResource } from "@/api/api";
import HttpMethodBadge from "./HttpMethodBadge";

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

interface Resource {
  id: string;
  name: string;
  description: string;
  baseUrl: string;
  httpMethod: HttpMethod;
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
          };
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
        urlParameters: [],
        headers: [],
        body: [],
      });

      setFormData({
        name: "",
        description: "",
        baseUrl: "",
        httpMethod: "GET",
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
    });
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
                  <HttpMethodBadge method={resource.httpMethod} />
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
    </section>
  );
}

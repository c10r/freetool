import { useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { Endpoint } from "../types";
import EndpointManager from "./EndpointManager";
import { getResources } from "@/api/api";

interface ResourcesViewProps {
  endpoints: Record<string, Endpoint>;
  createEndpoint: () => void;
  updateEndpoint: (ep: Endpoint) => void;
  deleteEndpoint: (id: string) => void;
}

interface Resource {
  id: string;
  name: string;
  description: string;
  baseUrl: string;
  httpMethod: string;
}

export default function ResourcesView({
  endpoints,
  createEndpoint,
  updateEndpoint,
  deleteEndpoint,
}: ResourcesViewProps) {
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchResources = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getResources();
      if (response.data?.items) {
        const mappedItems = response.data?.items.map((item) => {
          return {
            id: item.id,
            name: item.name,
            description: item.description,
            baseUrl: item.baseUrl,
            httpMethod: item.httpMethod,
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

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Resources</h2>
        <Button onClick={fetchResources}>Refresh</Button>
      </header>
      <Separator />

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
                  <span className="text-xs font-mono bg-gray-100 px-2 py-1 rounded">
                    {resource.httpMethod}
                  </span>
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

      <Card>
        <CardHeader>
          <CardTitle>Endpoint Manager</CardTitle>
        </CardHeader>
        <CardContent>
          <EndpointManager
            endpoints={endpoints}
            createEndpoint={createEndpoint}
            updateEndpoint={updateEndpoint}
            deleteEndpoint={deleteEndpoint}
          />
        </CardContent>
      </Card>
    </section>
  );
}

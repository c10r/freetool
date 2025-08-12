import { useState, useEffect } from "react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { EndpointMethod } from "../types";
import HttpMethodBadge from "./HttpMethodBadge";
import { getResources } from "@/api/api";

interface Resource {
  id: string;
  name: string;
  httpMethod: string;
}

interface ResourceSelectorProps {
  value?: string;
  onValueChange: (resourceId: string | undefined) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
}

export default function ResourceSelector({
  value,
  onValueChange,
  placeholder = "Select Resource",
  disabled = false,
  className,
}: ResourceSelectorProps) {
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchResources = async () => {
      setLoading(true);
      setError(null);
      try {
        const response = await getResources();
        if (response.error) {
          setError("Failed to load resources");
          setResources([]);
        } else if (response.data?.items) {
          const resourceList = response.data.items.map((item) => ({
            id: item.id!,
            name: item.name,
            httpMethod: item.httpMethod,
          }));
          setResources(resourceList);
        } else {
          setResources([]);
        }
      } catch (error) {
        setError("Failed to load resources");
        setResources([]);
      } finally {
        setLoading(false);
      }
    };

    fetchResources();
  }, []);

  const selectedResource = value
    ? resources.find((r) => r.id === value)
    : undefined;

  return (
    <Select
      value={value || ""}
      onValueChange={(v) => onValueChange(v || undefined)}
      disabled={disabled || loading}
    >
      <SelectTrigger className={className} aria-label="Select Resource">
        <SelectValue
          placeholder={
            loading
              ? "Loading resources..."
              : value && !selectedResource
                ? "Selected resource not found"
                : placeholder
          }
        />
      </SelectTrigger>
      <SelectContent>
        {resources.length > 0 ? (
          resources.map((resource) => (
            <SelectItem key={resource.id} value={resource.id}>
              <div className="flex items-center gap-2">
                <span>{resource.name}</span>
                <HttpMethodBadge
                  method={resource.httpMethod as EndpointMethod}
                />
              </div>
            </SelectItem>
          ))
        ) : (
          <div className="px-2 py-1.5 text-sm text-muted-foreground">
            {loading
              ? "Loading resources..."
              : error || "No resources available - Create one first"}
          </div>
        )}
      </SelectContent>
    </Select>
  );
}

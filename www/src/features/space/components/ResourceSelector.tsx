import { useEffect, useState } from "react";
import { getResources } from "@/api/api";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface Resource {
  id: string;
  name: string;
}

interface ResourceSelectorProps {
  spaceId: string;
  value?: string;
  onValueChange: (resourceId: string | undefined) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  id?: string;
  required?: boolean;
}

export default function ResourceSelector({
  spaceId,
  value,
  onValueChange,
  placeholder = "Select Resource",
  disabled = false,
  className,
  id,
  required = false,
}: ResourceSelectorProps) {
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchResources = async () => {
      setLoading(true);
      setError(null);
      try {
        const response = await getResources(spaceId);
        if (response.error) {
          setError("Failed to load resources");
          setResources([]);
        } else if (response.data?.items) {
          const resourceList = response.data.items.map((item) => ({
            id: item.id ?? "",
            name: item.name,
          }));
          setResources(resourceList);
        } else {
          setResources([]);
        }
      } catch (_error) {
        setError("Failed to load resources");
        setResources([]);
      } finally {
        setLoading(false);
      }
    };

    fetchResources();
  }, [spaceId]);

  const selectedResource = value
    ? resources.find((r) => r.id === value)
    : undefined;

  return (
    <Select
      value={value || ""}
      onValueChange={(v) => onValueChange(v || undefined)}
      disabled={disabled || loading}
    >
      <SelectTrigger
        id={id}
        className={className}
        aria-label="Select Resource"
        aria-required={required}
      >
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
              {resource.name}
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

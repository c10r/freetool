import { useEffect, useState } from "react";
import { getResources } from "@/api/api";
import httpLogo from "@/assets/http.svg";
import postgresLogo from "@/assets/postgres.png";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import type { ResourceKind } from "../types";

interface Resource {
  id: string;
  name: string;
  resourceKind: ResourceKind;
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
  readOnly?: boolean;
  readOnlyTooltip?: string;
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
  readOnly = false,
  readOnlyTooltip,
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
            resourceKind:
              item.resourceKind?.toLowerCase() === "sql" ? "sql" : "http",
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

  const getResourceLogo = (resourceKind: ResourceKind) =>
    resourceKind === "http" ? httpLogo : postgresLogo;

  const getResourceLogoAlt = (resourceKind: ResourceKind) =>
    resourceKind === "http" ? "HTTP" : "PostgreSQL";

  const showReadOnlyTooltip = readOnly && !!readOnlyTooltip;

  const trigger = (
    <SelectTrigger
      id={id}
      className={cn(className, readOnly && "cursor-not-allowed opacity-70")}
      aria-label="Select Resource"
      aria-required={required}
      aria-disabled={readOnly || disabled || loading}
    >
      {selectedResource ? (
        <div className="flex items-center gap-2">
          <div className="flex h-6 w-6 items-center justify-center rounded bg-gray-50 p-1">
            <img
              src={getResourceLogo(selectedResource.resourceKind)}
              alt={getResourceLogoAlt(selectedResource.resourceKind)}
              className="h-full w-full object-contain"
            />
          </div>
          <span>{selectedResource.name}</span>
        </div>
      ) : (
        <SelectValue
          placeholder={
            loading
              ? "Loading resources..."
              : value && !selectedResource
                ? "Selected resource not found"
                : placeholder
          }
        />
      )}
    </SelectTrigger>
  );

  return (
    <Select
      value={value || ""}
      onValueChange={(v) => {
        if (!readOnly) {
          onValueChange(v || undefined);
        }
      }}
      disabled={disabled || loading}
      open={readOnly ? false : undefined}
      onOpenChange={readOnly ? (_open) => undefined : undefined}
    >
      {showReadOnlyTooltip ? (
        <Tooltip>
          <TooltipTrigger asChild>{trigger}</TooltipTrigger>
          <TooltipContent className="max-w-sm">
            {readOnlyTooltip}
          </TooltipContent>
        </Tooltip>
      ) : (
        trigger
      )}
      <SelectContent>
        {resources.length > 0 ? (
          resources.map((resource) => (
            <SelectItem key={resource.id} value={resource.id}>
              <div className="flex items-center gap-2">
                <div className="flex h-6 w-6 items-center justify-center rounded bg-gray-50 p-1">
                  <img
                    src={getResourceLogo(resource.resourceKind)}
                    alt={getResourceLogoAlt(resource.resourceKind)}
                    className="h-full w-full object-contain"
                  />
                </div>
                <span>{resource.name}</span>
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

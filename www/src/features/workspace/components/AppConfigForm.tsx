import { useState, useEffect } from "react";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { KeyValuePair, EndpointMethod } from "../types";
import HttpMethodBadge from "./HttpMethodBadge";
import KeyValueList from "./KeyValueList";
import { getResources } from "@/api/api";
import { Check, X } from "lucide-react";

interface Resource {
  id: string;
  name: string;
  httpMethod: string;
  urlParameters?: KeyValuePair[];
  headers?: KeyValuePair[];
  body?: KeyValuePair[];
}

interface FieldState {
  updating: boolean;
  saved: boolean;
  error: boolean;
  errorMessage: string;
}

interface AppConfigFormProps {
  resourceId?: string;
  queryParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
  onResourceChange: (resourceId: string | undefined) => void;
  onQueryParametersChange: (queryParameters: KeyValuePair[]) => void;
  onHeadersChange: (headers: KeyValuePair[]) => void;
  onBodyChange: (body: KeyValuePair[]) => void;
  disabled?: boolean;
  showResourceSelector?: boolean;
  resourceSelectorLabel?: string;
  mode?: "create" | "edit";
  fieldStates?: {
    queryParameters: FieldState;
    headers: FieldState;
    body: FieldState;
  };
  onKeyValueFieldBlur?: (
    field: "queryParameters" | "headers" | "body",
    value: KeyValuePair[],
  ) => void;
}

const FieldIndicator = ({ state }: { state: FieldState }) => {
  if (state.updating) {
    return (
      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
    );
  }
  if (state.saved && !state.error) {
    return <Check className="h-4 w-4 text-green-500" />;
  }
  if (state.error) {
    return <X className="h-4 w-4 text-red-500" />;
  }
  return null;
};

export default function AppConfigForm({
  resourceId,
  queryParameters,
  headers,
  body,
  onResourceChange,
  onQueryParametersChange,
  onHeadersChange,
  onBodyChange,
  disabled = false,
  showResourceSelector = true,
  resourceSelectorLabel = "Resource",
  mode = "create",
  fieldStates,
  onKeyValueFieldBlur,
}: AppConfigFormProps) {
  const [resources, setResources] = useState<Resource[]>([]);
  const [loadingResources, setLoadingResources] = useState(false);
  const [resourcesError, setResourcesError] = useState<string | null>(null);

  const selectedResource = resourceId
    ? resources.find((r) => r.id === resourceId)
    : undefined;

  const getFieldState = (
    field: "queryParameters" | "headers" | "body",
  ): FieldState => {
    const state = fieldStates?.[field] || {
      updating: false,
      saved: false,
      error: false,
      errorMessage: "",
    };
    return state;
  };

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
            urlParameters: item.urlParameters || [],
            headers: item.headers || [],
            body: item.body || [],
          }));
          setResources(resourceList);
        } else {
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

  return (
    <div className="space-y-4">
      {showResourceSelector && (
        <div className="space-y-2">
          <Label htmlFor="resource-select">{resourceSelectorLabel}</Label>
          <Select
            value={resourceId || ""}
            onValueChange={(value) => onResourceChange(value || undefined)}
            disabled={disabled || loadingResources}
          >
            <SelectTrigger id="resource-select">
              <SelectValue
                placeholder={
                  loadingResources
                    ? "Loading resources..."
                    : resourceId && !selectedResource
                      ? "Selected resource not found"
                      : "Select a resource"
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
                  {loadingResources
                    ? "Loading resources..."
                    : resourcesError ||
                      "No resources available - Create one first"}
                </div>
              )}
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="space-y-2">
        <Label>Query Parameters</Label>
        {selectedResource &&
          selectedResource.urlParameters &&
          selectedResource.urlParameters.length > 0 && (
            <KeyValueList
              items={selectedResource.urlParameters}
              onChange={() => {}} // No-op for read-only
              ariaLabel="Query parameters"
              readOnly
              readOnlyLabel="From selected resource (read-only):"
            />
          )}
        <div className="relative">
          <KeyValueList
            items={queryParameters}
            onChange={onQueryParametersChange}
            onBlur={(items) => {
              onKeyValueFieldBlur?.("queryParameters", items);
            }}
            ariaLabel="Query parameters"
            disabled={disabled || getFieldState("queryParameters").updating}
          />
          {mode === "edit" && (
            <div className="absolute right-3 top-3">
              <FieldIndicator state={getFieldState("queryParameters")} />
            </div>
          )}
        </div>
        {getFieldState("queryParameters").errorMessage && (
          <div className="text-red-500 text-sm mt-1">
            {getFieldState("queryParameters").errorMessage}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label>Headers</Label>
        {selectedResource &&
          selectedResource.headers &&
          selectedResource.headers.length > 0 && (
            <KeyValueList
              items={selectedResource.headers}
              onChange={() => {}} // No-op for read-only
              ariaLabel="Headers"
              readOnly
              readOnlyLabel="From selected resource (read-only):"
            />
          )}
        <div className="relative">
          <KeyValueList
            items={headers}
            onChange={onHeadersChange}
            onBlur={(items) => {
              onKeyValueFieldBlur?.("headers", items);
            }}
            ariaLabel="Headers"
            disabled={disabled || getFieldState("headers").updating}
          />
          {mode === "edit" && (
            <div className="absolute right-3 top-3">
              <FieldIndicator state={getFieldState("headers")} />
            </div>
          )}
        </div>
        {getFieldState("headers").errorMessage && (
          <div className="text-red-500 text-sm mt-1">
            {getFieldState("headers").errorMessage}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label>JSON Body</Label>
        {selectedResource &&
          selectedResource.body &&
          selectedResource.body.length > 0 && (
            <KeyValueList
              items={selectedResource.body}
              onChange={() => {}} // No-op for read-only
              ariaLabel="JSON body"
              readOnly
              readOnlyLabel="From selected resource (read-only):"
            />
          )}
        <div className="relative">
          <KeyValueList
            items={body}
            onChange={onBodyChange}
            onBlur={(items) => {
              onKeyValueFieldBlur?.("body", items);
            }}
            ariaLabel="JSON body"
            disabled={disabled || getFieldState("body").updating}
          />
          {mode === "edit" && (
            <div className="absolute right-3 top-3">
              <FieldIndicator state={getFieldState("body")} />
            </div>
          )}
        </div>
        {getFieldState("body").errorMessage && (
          <div className="text-red-500 text-sm mt-1">
            {getFieldState("body").errorMessage}
          </div>
        )}
      </div>
    </div>
  );
}

import { Check, X } from "lucide-react";
import { useEffect, useState } from "react";
import { getResources } from "@/api/api";
import { Input } from "@/components/ui/input";
import { InputWithPlaceholders } from "@/components/ui/input-with-placeholders";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { EndpointMethod, KeyValuePair } from "../types";
import HttpMethodBadge from "./HttpMethodBadge";
import KeyValueList from "./KeyValueList";

interface Resource {
  id: string;
  name: string;
  httpMethod: string;
  baseUrl?: string;
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

interface AppInput {
  title?: string | null;
  required?: boolean;
}

interface AppConfigFormProps {
  resourceId?: string;
  urlPath?: string;
  queryParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
  onResourceChange: (resourceId: string | undefined) => void;
  onUrlPathChange?: (urlPath: string) => void;
  onQueryParametersChange: (urlParameters: KeyValuePair[]) => void;
  onHeadersChange: (headers: KeyValuePair[]) => void;
  onBodyChange: (body: KeyValuePair[]) => void;
  disabled?: boolean;
  showResourceSelector?: boolean;
  resourceSelectorLabel?: string;
  mode?: "create" | "edit";
  fieldStates?: {
    urlPath?: FieldState;
    urlParameters: FieldState;
    headers: FieldState;
    body: FieldState;
  };
  onKeyValueFieldBlur?: (
    field: "urlParameters" | "headers" | "body",
    value: KeyValuePair[]
  ) => void;
  onUrlPathFieldBlur?: (value: string) => void;
  inputs?: AppInput[];
}

const FieldIndicator = ({ state }: { state: FieldState }) => {
  if (state.updating) {
    return (
      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900" />
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
  urlPath = "",
  queryParameters,
  headers,
  body,
  onResourceChange,
  onUrlPathChange,
  onQueryParametersChange,
  onHeadersChange,
  onBodyChange,
  disabled = false,
  showResourceSelector = true,
  resourceSelectorLabel = "Resource",
  mode = "create",
  fieldStates,
  onKeyValueFieldBlur,
  onUrlPathFieldBlur,
  inputs,
}: AppConfigFormProps) {
  const [resources, setResources] = useState<Resource[]>([]);
  const [loadingResources, setLoadingResources] = useState(false);
  const [resourcesError, setResourcesError] = useState<string | null>(null);

  const selectedResource = resourceId
    ? resources.find((r) => r.id === resourceId)
    : undefined;

  const getFieldState = (
    field: "urlPath" | "urlParameters" | "headers" | "body"
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
            id: item.id ?? "",
            name: item.name,
            httpMethod: item.httpMethod,
            baseUrl: item.baseUrl,
            urlParameters: (item.urlParameters || []).map((kvp) => ({
              key: kvp.key || "",
              value: kvp.value || "",
            })),
            headers: (item.headers || []).map((kvp) => ({
              key: kvp.key || "",
              value: kvp.value || "",
            })),
            body: (item.body || []).map((kvp) => ({
              key: kvp.key || "",
              value: kvp.value || "",
            })),
          }));
          setResources(resourceList);
        } else {
          setResources([]);
        }
      } catch (_error) {
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

      {selectedResource && (
        <div className="space-y-2">
          <Label>URL</Label>
          <div className="flex gap-2">
            <div className="flex-1">
              <Label
                htmlFor="base-url"
                className="text-sm text-muted-foreground"
              >
                Base URL (from resource)
              </Label>
              <Input
                id="base-url"
                value={
                  selectedResource.baseUrl
                    ? selectedResource.baseUrl.substring(0, 50) +
                      (selectedResource.baseUrl.length > 50 ? "..." : "")
                    : ""
                }
                disabled
                className="bg-muted"
              />
            </div>
            <div className="flex-1">
              <Label htmlFor="url-path" className="text-sm">
                Custom Path
              </Label>
              <div className="relative">
                {inputs && inputs.length > 0 ? (
                  <InputWithPlaceholders
                    id="url-path"
                    value={urlPath}
                    onChange={(value) => onUrlPathChange?.(value)}
                    onBlur={() => onUrlPathFieldBlur?.(urlPath)}
                    availableInputs={inputs}
                    placeholder="Example: /api/users/{id}"
                    disabled={disabled || getFieldState("urlPath").updating}
                  />
                ) : (
                  <Input
                    id="url-path"
                    value={urlPath}
                    onChange={(e) => onUrlPathChange?.(e.target.value)}
                    onBlur={(e) => onUrlPathFieldBlur?.(e.target.value)}
                    placeholder="Example: /api/users"
                    disabled={disabled || getFieldState("urlPath").updating}
                  />
                )}
                {mode === "edit" && (
                  <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                    <FieldIndicator state={getFieldState("urlPath")} />
                  </div>
                )}
              </div>
              {getFieldState("urlPath").errorMessage && (
                <div className="text-red-500 text-sm mt-1">
                  {getFieldState("urlPath").errorMessage}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      <div className="space-y-2">
        <Label>Query Parameters</Label>
        {selectedResource?.urlParameters &&
          selectedResource.urlParameters.length > 0 && (
            <KeyValueList
              items={selectedResource.urlParameters}
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
              onKeyValueFieldBlur?.("urlParameters", items);
            }}
            ariaLabel="Query parameters"
            disabled={disabled || getFieldState("urlParameters").updating}
            availableInputs={inputs}
          />
          {mode === "edit" && (
            <div className="absolute right-3 top-3">
              <FieldIndicator state={getFieldState("urlParameters")} />
            </div>
          )}
        </div>
        {getFieldState("urlParameters").errorMessage && (
          <div className="text-red-500 text-sm mt-1">
            {getFieldState("urlParameters").errorMessage}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label>Headers</Label>
        {selectedResource?.headers && selectedResource.headers.length > 0 && (
          <KeyValueList
            items={selectedResource.headers}
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
            availableInputs={inputs}
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
        {selectedResource?.body && selectedResource.body.length > 0 && (
          <KeyValueList
            items={selectedResource.body}
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
            availableInputs={inputs}
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

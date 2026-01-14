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
import { Switch } from "@/components/ui/switch";
import type { EndpointMethod, KeyValuePair } from "../types";
import HttpMethodBadge from "./HttpMethodBadge";
import KeyValueList from "./KeyValueList";

interface Resource {
  id: string;
  name: string;
  baseUrl?: string;
  urlParameters?: KeyValuePair[];
  headers?: KeyValuePair[];
  body?: KeyValuePair[];
}

interface AppInput {
  title?: string | null;
  required?: boolean;
}

interface AppConfigFormProps {
  spaceId?: string;
  resourceId?: string;
  urlPath?: string;
  httpMethod?: EndpointMethod;
  queryParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
  useDynamicJsonBody?: boolean;
  onResourceChange: (resourceId: string | undefined) => void;
  onUrlPathChange?: (urlPath: string) => void;
  onHttpMethodChange?: (method: EndpointMethod) => void;
  onQueryParametersChange: (urlParameters: KeyValuePair[]) => void;
  onHeadersChange: (headers: KeyValuePair[]) => void;
  onBodyChange: (body: KeyValuePair[]) => void;
  onUseDynamicJsonBodyChange?: (useDynamicJsonBody: boolean) => void;
  disabled?: boolean;
  showResourceSelector?: boolean;
  resourceSelectorLabel?: string;
  mode?: "create" | "edit";
  inputs?: AppInput[];
}

export default function AppConfigForm({
  spaceId,
  resourceId,
  urlPath = "",
  httpMethod,
  queryParameters,
  headers,
  body,
  useDynamicJsonBody = false,
  onResourceChange,
  onUrlPathChange,
  onHttpMethodChange,
  onQueryParametersChange,
  onHeadersChange,
  onBodyChange,
  onUseDynamicJsonBodyChange,
  disabled = false,
  showResourceSelector = true,
  resourceSelectorLabel = "Resource",
  inputs,
}: AppConfigFormProps) {
  const [resources, setResources] = useState<Resource[]>([]);
  const [loadingResources, setLoadingResources] = useState(false);
  const [resourcesError, setResourcesError] = useState<string | null>(null);

  const selectedResource = resourceId
    ? resources.find((r) => r.id === resourceId)
    : undefined;

  // App inputs for the "App Fields" section
  // (InputWithPlaceholders always shows current_user in "User Context" section)
  const appInputs = inputs || [];

  useEffect(() => {
    const fetchResources = async () => {
      if (!spaceId) {
        setResources([]);
        return;
      }

      setLoadingResources(true);
      setResourcesError(null);
      try {
        const response = await getResources(spaceId);
        if (response.error) {
          setResourcesError("Failed to load resources");
          setResources([]);
        } else if (response.data?.items) {
          const resourceList = response.data.items.map((item) => ({
            id: item.id ?? "",
            name: item.name,
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
  }, [spaceId]);

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
                    {resource.name}
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

      {/* HTTP Method Selector */}
      <div className="space-y-2">
        <Label htmlFor="http-method">HTTP Method *</Label>
        <Select
          value={httpMethod || ""}
          onValueChange={(value) => {
            const method = value as EndpointMethod;
            onHttpMethodChange?.(method);
          }}
          disabled={disabled}
        >
          <SelectTrigger id="http-method">
            <SelectValue placeholder="Select HTTP method" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="GET">
              <HttpMethodBadge method="GET" />
            </SelectItem>
            <SelectItem value="POST">
              <HttpMethodBadge method="POST" />
            </SelectItem>
            <SelectItem value="PUT">
              <HttpMethodBadge method="PUT" />
            </SelectItem>
            <SelectItem value="PATCH">
              <HttpMethodBadge method="PATCH" />
            </SelectItem>
            <SelectItem value="DELETE">
              <HttpMethodBadge method="DELETE" />
            </SelectItem>
          </SelectContent>
        </Select>
      </div>

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
              <InputWithPlaceholders
                id="url-path"
                value={urlPath}
                onChange={(value) => onUrlPathChange?.(value)}
                availableInputs={appInputs}
                placeholder="Example: /api/users/{id}"
                disabled={disabled}
              />
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
        <KeyValueList
          items={queryParameters}
          onChange={onQueryParametersChange}
          ariaLabel="Query parameters"
          disabled={disabled}
          availableInputs={appInputs}
        />
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
        <KeyValueList
          items={headers}
          onChange={onHeadersChange}
          ariaLabel="Headers"
          disabled={disabled}
          availableInputs={appInputs}
        />
      </div>

      <div className="space-y-2">
        <Label>JSON Body</Label>

        {/* Dynamic JSON Body Toggle */}
        <div className="flex items-center justify-between p-3 bg-muted/50 rounded-lg">
          <div className="space-y-0.5">
            <Label
              htmlFor="dynamic-body-toggle"
              className="text-sm font-medium cursor-pointer"
            >
              Dynamic JSON Body
            </Label>
            <p className="text-xs text-muted-foreground">
              Users provide body key-value pairs at runtime instead of
              predefined values
            </p>
          </div>
          <Switch
            id="dynamic-body-toggle"
            checked={useDynamicJsonBody}
            onCheckedChange={(checked) => {
              onUseDynamicJsonBodyChange?.(checked);
            }}
            disabled={disabled}
          />
        </div>

        {/* Static body editor - only shown when dynamic body is disabled */}
        {!useDynamicJsonBody && (
          <>
            {selectedResource?.body && selectedResource.body.length > 0 && (
              <KeyValueList
                items={selectedResource.body}
                ariaLabel="JSON body"
                readOnly
                readOnlyLabel="From selected resource (read-only):"
              />
            )}
            <KeyValueList
              items={body}
              onChange={onBodyChange}
              ariaLabel="JSON body"
              disabled={disabled}
              availableInputs={appInputs}
            />
          </>
        )}

        {useDynamicJsonBody && (
          <p className="text-sm text-muted-foreground italic">
            Body parameters will be provided by users when running this app (max
            10 key-value pairs).
          </p>
        )}
      </div>
    </div>
  );
}

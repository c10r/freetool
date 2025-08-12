import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { KeyValuePair } from "../types";
import KeyValueList from "./KeyValueList";
import HttpMethodBadge from "./HttpMethodBadge";
import { Check, X } from "lucide-react";

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface ResourceFormData {
  name: string;
  description: string;
  baseUrl: string;
  httpMethod: HttpMethod;
  urlParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
}

interface FieldState {
  updating: boolean;
  saved: boolean;
  error: boolean;
  errorMessage: string;
}

interface ResourceFormProps {
  data: ResourceFormData;
  onChange: (data: ResourceFormData) => void;
  mode: "create" | "edit";
  disabled?: boolean;
  fieldStates?: {
    name: FieldState;
    description: FieldState;
    baseUrl: FieldState;
    urlParameters: FieldState;
    headers: FieldState;
    body: FieldState;
  };
  onFieldBlur?: (field: keyof ResourceFormData, value: any) => void;
  onKeyValueFieldBlur?: (
    field: "urlParameters" | "headers" | "body",
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

export default function ResourceForm({
  data,
  onChange,
  mode,
  disabled = false,
  fieldStates,
  onFieldBlur,
  onKeyValueFieldBlur,
}: ResourceFormProps) {
  const updateData = (field: keyof ResourceFormData, value: any) => {
    onChange({ ...data, [field]: value });
  };

  const getFieldState = (field: keyof ResourceFormData): FieldState => {
    const state = fieldStates?.[field] || {
      updating: false,
      saved: false,
      error: false,
      errorMessage: "",
    };
    return state;
  };

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="name">Name *</Label>
          <div className="relative">
            <Input
              id="name"
              placeholder="Enter resource name"
              value={data.name}
              onChange={(e) => {
                updateData("name", e.target.value);
              }}
              onBlur={(e) => onFieldBlur?.("name", e.target.value)}
              disabled={disabled || getFieldState("name").updating}
              style={{
                backgroundColor:
                  disabled || getFieldState("name").updating
                    ? "#f3f4f6"
                    : "white",
              }}
            />
            {mode === "edit" && (
              <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                <FieldIndicator state={getFieldState("name")} />
              </div>
            )}
          </div>
          {getFieldState("name").errorMessage && (
            <div className="text-red-500 text-sm mt-1">
              {getFieldState("name").errorMessage}
            </div>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="httpMethod">HTTP Method</Label>
          {mode === "create" ? (
            <Select
              value={data.httpMethod}
              onValueChange={(value) =>
                updateData("httpMethod", value as HttpMethod)
              }
              disabled={disabled}
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
          ) : (
            <div className="flex items-center gap-2">
              <HttpMethodBadge method={data.httpMethod} />
              <span className="text-sm text-muted-foreground">
                (HTTP method cannot be changed after creation)
              </span>
            </div>
          )}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="baseUrl">Base URL *</Label>
        <div className="relative">
          <Input
            id="baseUrl"
            placeholder="https://api.example.com"
            value={data.baseUrl}
            onChange={(e) => updateData("baseUrl", e.target.value)}
            onBlur={(e) => onFieldBlur?.("baseUrl", e.target.value)}
            disabled={disabled || getFieldState("baseUrl").updating}
          />
          {mode === "edit" && (
            <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
              <FieldIndicator state={getFieldState("baseUrl")} />
            </div>
          )}
        </div>
        {getFieldState("baseUrl").errorMessage && (
          <div className="text-red-500 text-sm mt-1">
            {getFieldState("baseUrl").errorMessage}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description *</Label>
        <div className="relative">
          <Textarea
            id="description"
            placeholder="Enter resource description"
            value={data.description}
            onChange={(e) => updateData("description", e.target.value)}
            onBlur={(e) => onFieldBlur?.("description", e.target.value)}
            disabled={disabled || getFieldState("description").updating}
            rows={3}
          />
          {mode === "edit" && (
            <div className="absolute right-3 top-3">
              <FieldIndicator state={getFieldState("description")} />
            </div>
          )}
        </div>
        {getFieldState("description").errorMessage && (
          <div className="text-red-500 text-sm mt-1">
            {getFieldState("description").errorMessage}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label>Query Parameters</Label>
        <div className="relative">
          <KeyValueList
            items={data.urlParameters}
            onChange={(items) => updateData("urlParameters", items)}
            onBlur={(items) => onKeyValueFieldBlur?.("urlParameters", items)}
            ariaLabel="URL parameters"
            disabled={disabled || getFieldState("urlParameters").updating}
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
        <Label>HTTP Headers</Label>
        <div className="relative">
          <KeyValueList
            items={data.headers}
            onChange={(items) => updateData("headers", items)}
            onBlur={(items) => onKeyValueFieldBlur?.("headers", items)}
            ariaLabel="HTTP headers"
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
        <div className="relative">
          <KeyValueList
            items={data.body}
            onChange={(items) => updateData("body", items)}
            onBlur={(items) => onKeyValueFieldBlur?.("body", items)}
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

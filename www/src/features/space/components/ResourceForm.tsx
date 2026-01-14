import { Input } from "@/components/ui/input";
import { getCurrentUserInputs } from "@/components/ui/input-with-placeholders/current-user";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { AuthConfig, KeyValuePair } from "../types";
import AuthorizationSection from "./AuthorizationSection";
import KeyValueList from "./KeyValueList";

// Get current_user inputs for Resources (always available)
const currentUserInputs = getCurrentUserInputs();

export interface ResourceFormData {
  name: string;
  description: string;
  baseUrl: string;
  urlParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
  authConfig: AuthConfig;
}

interface ResourceFormProps {
  data: ResourceFormData;
  onChange: (data: ResourceFormData) => void;
  mode: "create" | "edit";
  disabled?: boolean;
}

export default function ResourceForm({
  data,
  onChange,
  mode: _mode,
  disabled = false,
}: ResourceFormProps) {
  const updateData = (
    field: keyof ResourceFormData,
    value: string | KeyValuePair[]
  ) => {
    onChange({ ...data, [field]: value });
  };

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="name">Name *</Label>
        <Input
          id="name"
          placeholder="Enter resource name"
          value={data.name}
          onChange={(e) => {
            updateData("name", e.target.value);
          }}
          disabled={disabled}
          style={{
            backgroundColor: disabled ? "#f3f4f6" : "white",
          }}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="baseUrl">Base URL *</Label>
        <Input
          id="baseUrl"
          placeholder="https://api.example.com"
          value={data.baseUrl}
          onChange={(e) => updateData("baseUrl", e.target.value)}
          disabled={disabled}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description *</Label>
        <Textarea
          id="description"
          placeholder="Enter resource description"
          value={data.description}
          onChange={(e) => updateData("description", e.target.value)}
          disabled={disabled}
          rows={3}
        />
      </div>

      <div className="space-y-2">
        <Label>Query Parameters</Label>
        <KeyValueList
          items={data.urlParameters}
          onChange={(items) => updateData("urlParameters", items)}
          ariaLabel="URL parameters"
          disabled={disabled}
          availableInputs={currentUserInputs}
        />
      </div>

      <div className="space-y-2">
        <Label>HTTP Headers</Label>
        <KeyValueList
          items={data.headers}
          onChange={(items) => updateData("headers", items)}
          ariaLabel="HTTP headers"
          disabled={disabled}
          availableInputs={currentUserInputs}
          blockedKeys={["Authorization"]}
        />
      </div>

      <div className="space-y-2">
        <Label>JSON Body</Label>
        <KeyValueList
          items={data.body}
          onChange={(items) => updateData("body", items)}
          ariaLabel="JSON body"
          disabled={disabled}
          availableInputs={currentUserInputs}
        />
      </div>

      <div className="space-y-2">
        <AuthorizationSection
          authConfig={data.authConfig}
          onChange={(authConfig) => onChange({ ...data, authConfig })}
          disabled={disabled}
        />
      </div>
    </div>
  );
}

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { AuthConfig, AuthType } from "../types";

interface AuthorizationSectionProps {
  authConfig: AuthConfig;
  onChange: (config: AuthConfig) => void;
  onBlur?: () => void;
  disabled?: boolean;
}

export default function AuthorizationSection({
  authConfig,
  onChange,
  onBlur,
  disabled = false,
}: AuthorizationSectionProps) {
  const handleTypeChange = (newType: AuthType) => {
    switch (newType) {
      case "none":
        onChange({ type: "none" });
        break;
      case "basic":
        onChange({ type: "basic", username: "", password: "" });
        break;
      case "bearer":
        onChange({ type: "bearer", token: "" });
        break;
    }
  };

  return (
    <div className="space-y-3">
      <Label>Authorization</Label>

      <Select
        value={authConfig.type}
        onValueChange={(value) => handleTypeChange(value as AuthType)}
        disabled={disabled}
      >
        <SelectTrigger aria-label="Authorization type">
          <SelectValue placeholder="Select auth type" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="none">No Auth</SelectItem>
          <SelectItem value="bearer">Bearer Token</SelectItem>
          <SelectItem value="basic">Basic Auth</SelectItem>
        </SelectContent>
      </Select>

      {authConfig.type === "bearer" && (
        <div className="space-y-2">
          <Input
            type="password"
            placeholder="Enter token"
            value={authConfig.token}
            onChange={(e) =>
              onChange({ type: "bearer", token: e.target.value })
            }
            onBlur={onBlur}
            disabled={disabled}
            aria-label="Bearer token"
          />
        </div>
      )}

      {authConfig.type === "basic" && (
        <div className="grid grid-cols-2 gap-3">
          <Input
            type="text"
            placeholder="Username"
            value={authConfig.username}
            onChange={(e) =>
              onChange({
                type: "basic",
                username: e.target.value,
                password: authConfig.password,
              })
            }
            onBlur={onBlur}
            disabled={disabled}
            aria-label="Username"
          />
          <Input
            type="password"
            placeholder="Password"
            value={authConfig.password}
            onChange={(e) =>
              onChange({
                type: "basic",
                username: authConfig.username,
                password: e.target.value,
              })
            }
            onBlur={onBlur}
            disabled={disabled}
            aria-label="Password"
          />
        </div>
      )}
    </div>
  );
}

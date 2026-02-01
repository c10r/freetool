import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { getCurrentUserInputs } from "@/components/ui/input-with-placeholders/current-user";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import type {
  AuthConfig,
  DatabaseConfig,
  KeyValuePair,
  ResourceKind,
} from "../types";
import AuthorizationSection from "./AuthorizationSection";
import KeyValueList from "./KeyValueList";

// Get current_user inputs for Resources (always available)
const currentUserInputs = getCurrentUserInputs();

export interface ResourceFormData {
  name: string;
  description: string;
  resourceKind: ResourceKind;
  baseUrl: string;
  urlParameters: KeyValuePair[];
  headers: KeyValuePair[];
  body: KeyValuePair[];
  authConfig: AuthConfig;
  databaseConfig: DatabaseConfig;
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
  mode,
  disabled = false,
}: ResourceFormProps) {
  const [connectionString, setConnectionString] = useState("");
  const [connectionStringError, setConnectionStringError] = useState<
    string | null
  >(null);

  const updateData = (
    field: keyof ResourceFormData,
    value: ResourceFormData[keyof ResourceFormData]
  ) => {
    onChange({ ...data, [field]: value });
  };

  const updateDatabaseConfig = (
    field: keyof DatabaseConfig,
    value: DatabaseConfig[keyof DatabaseConfig]
  ) => {
    onChange({
      ...data,
      databaseConfig: {
        ...data.databaseConfig,
        [field]: value,
      },
    });
  };

  const parseConnectionString = () => {
    const trimmed = connectionString.trim();
    if (!trimmed) {
      setConnectionStringError("Connection string is required");
      return;
    }

    try {
      const url = new URL(trimmed);
      const params = new URLSearchParams(url.search);
      const nextOptions: KeyValuePair[] = [];
      let nextUseSsl = data.databaseConfig.useSsl;

      params.forEach((value, key) => {
        const normalizedKey = key.toLowerCase();
        const normalizedValue = value.toLowerCase();

        if (normalizedKey === "ssl" || normalizedKey === "tls") {
          nextUseSsl =
            normalizedValue === "true" ||
            normalizedValue === "1" ||
            normalizedValue === "require" ||
            normalizedValue === "required";
          return;
        }

        if (normalizedKey === "sslmode") {
          nextUseSsl =
            normalizedValue === "require" ||
            normalizedValue === "verify-ca" ||
            normalizedValue === "verify-full";
          return;
        }

        nextOptions.push({ key, value });
      });

      const databaseName = decodeURIComponent(url.pathname.replace(/^\/+/, ""));
      const portValue = url.port ? String(url.port) : "";

      updateDatabaseConfig("databaseName", databaseName);
      updateDatabaseConfig("host", url.hostname);
      updateDatabaseConfig("port", portValue);
      updateDatabaseConfig("engine", "postgres");
      updateDatabaseConfig("authScheme", "username_password");
      updateDatabaseConfig("username", decodeURIComponent(url.username));
      updateDatabaseConfig("password", decodeURIComponent(url.password));
      updateDatabaseConfig("useSsl", nextUseSsl);
      updateDatabaseConfig("connectionOptions", nextOptions);

      setConnectionStringError(null);
    } catch (_error) {
      setConnectionStringError("Unable to parse connection string");
    }
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
        <Label htmlFor="resourceKind">Resource Kind *</Label>
        <Select
          value={data.resourceKind}
          onValueChange={(value) =>
            updateData("resourceKind", value as ResourceKind)
          }
          disabled={disabled || mode === "edit"}
        >
          <SelectTrigger id="resourceKind">
            <SelectValue placeholder="Select resource kind" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="http">HTTP</SelectItem>
            <SelectItem value="sql">SQL</SelectItem>
          </SelectContent>
        </Select>
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

      {data.resourceKind === "http" ? (
        <>
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
        </>
      ) : (
        <>
          <div className="space-y-2">
            <Label htmlFor="connectionString">Connection String</Label>
            <div className="flex gap-2">
              <Input
                id="connectionString"
                placeholder="postgres://user:pass@host:5432/dbname"
                value={connectionString}
                onChange={(e) => setConnectionString(e.target.value)}
                disabled={disabled}
              />
              <Button
                type="button"
                onClick={parseConnectionString}
                disabled={disabled}
                variant="outline"
              >
                Parse
              </Button>
            </div>
            {connectionStringError ? (
              <p className="text-xs text-destructive">
                {connectionStringError}
              </p>
            ) : null}
          </div>

          <div className="space-y-2">
            <Label htmlFor="databaseName">Database Name *</Label>
            <Input
              id="databaseName"
              placeholder="analytics"
              value={data.databaseConfig.databaseName}
              onChange={(e) =>
                updateDatabaseConfig("databaseName", e.target.value)
              }
              disabled={disabled}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="databaseEngine">Database Engine *</Label>
            <Select
              value={data.databaseConfig.engine}
              onValueChange={(value) =>
                updateDatabaseConfig(
                  "engine",
                  value as DatabaseConfig["engine"]
                )
              }
              disabled={disabled}
            >
              <SelectTrigger id="databaseEngine">
                <SelectValue placeholder="Select engine" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="postgres">Postgres</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="databaseHost">Host *</Label>
              <Input
                id="databaseHost"
                placeholder="db.internal"
                value={data.databaseConfig.host}
                onChange={(e) => updateDatabaseConfig("host", e.target.value)}
                disabled={disabled}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="databasePort">Port *</Label>
              <Input
                id="databasePort"
                placeholder="5432"
                value={data.databaseConfig.port}
                onChange={(e) => updateDatabaseConfig("port", e.target.value)}
                disabled={disabled}
                inputMode="numeric"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="authScheme">Auth Scheme</Label>
            <Select
              value={data.databaseConfig.authScheme}
              onValueChange={(value) =>
                updateDatabaseConfig(
                  "authScheme",
                  value as DatabaseConfig["authScheme"]
                )
              }
              disabled={disabled}
            >
              <SelectTrigger id="authScheme">
                <SelectValue placeholder="Select auth scheme" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="username_password">
                  Username + Password
                </SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="databaseUsername">Username *</Label>
              <Input
                id="databaseUsername"
                placeholder="db_user"
                value={data.databaseConfig.username}
                onChange={(e) =>
                  updateDatabaseConfig("username", e.target.value)
                }
                disabled={disabled}
                autoComplete="off"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="databasePassword">Password *</Label>
              <Input
                id="databasePassword"
                placeholder="••••••••"
                type="password"
                value={data.databaseConfig.password}
                onChange={(e) =>
                  updateDatabaseConfig("password", e.target.value)
                }
                disabled={disabled}
                autoComplete="new-password"
              />
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="flex items-center justify-between rounded-md border p-3">
              <div>
                <Label htmlFor="useSsl">Use SSL/TLS</Label>
                <p className="text-xs text-muted-foreground">
                  Encrypt connections to the database
                </p>
              </div>
              <Switch
                id="useSsl"
                checked={data.databaseConfig.useSsl}
                onCheckedChange={(value) =>
                  updateDatabaseConfig("useSsl", value)
                }
                disabled={disabled}
              />
            </div>
            <div className="flex items-center justify-between rounded-md border p-3">
              <div>
                <Label htmlFor="enableSshTunnel">Enable SSH Tunnel</Label>
                <p className="text-xs text-muted-foreground">
                  Connect through an SSH bastion
                </p>
              </div>
              <Switch
                id="enableSshTunnel"
                checked={data.databaseConfig.enableSshTunnel}
                onCheckedChange={(value) =>
                  updateDatabaseConfig("enableSshTunnel", value)
                }
                disabled={disabled}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label>Connection Options</Label>
            <KeyValueList
              items={data.databaseConfig.connectionOptions}
              onChange={(items) =>
                updateDatabaseConfig("connectionOptions", items)
              }
              ariaLabel="Connection options"
              disabled={disabled}
            />
          </div>
        </>
      )}
    </div>
  );
}

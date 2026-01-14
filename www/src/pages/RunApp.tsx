import {
  AlertCircle,
  ArrowLeft,
  ChevronDown,
  ChevronRight,
  Loader,
  Play,
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { getAppById, getFolderById, runApp } from "@/api/api";
import { PermissionButton } from "@/components/PermissionButton";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Switch } from "@/components/ui/switch";
import DynamicBodyEditor from "@/features/space/components/DynamicBodyEditor";
import { useHasPermission } from "@/hooks/usePermissions";
import {
  extractDefaultValue,
  fromBackendInputType,
  getRadioOptionsFromBackendType,
} from "@/lib/inputTypeMapper";
import type { components } from "@/schema";

type AppData = components["schemas"]["AppData"];
type RunData = components["schemas"]["RunData"];
type KeyValuePair = components["schemas"]["KeyValuePair"];
type AppInput = components["schemas"]["Input"];

// Helper function to validate email format
const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

// Helper function to safely format response content
const formatResponse = (
  response: string
): { content: string; isJson: boolean } => {
  try {
    const parsed = JSON.parse(response);
    return {
      content: JSON.stringify(parsed, null, 2),
      isJson: true,
    };
  } catch {
    return {
      content: response,
      isJson: false,
    };
  }
};

const RunApp = () => {
  const { nodeId } = useParams<{ nodeId: string }>();
  const navigate = useNavigate();

  const [app, setApp] = useState<AppData | null>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<RunData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [runError, setRunError] = useState<string | null>(null);
  const [isRequestCollapsed, setIsRequestCollapsed] = useState(true);
  const [spaceId, setSpaceId] = useState("");
  const [spaceLoading, setSpaceLoading] = useState(false);
  const [inputValues, setInputValues] = useState<Record<string, string>>({});
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [dynamicBody, setDynamicBody] = useState<
    { key: string; value: string }[]
  >([{ key: "", value: "" }]);
  const [dynamicBodyError, setDynamicBodyError] = useState<string | null>(null);

  const canRunApp = useHasPermission(spaceId, "run_app");

  // Check if app has inputs that need to be filled
  const hasInputs = app?.inputs && app.inputs.length > 0;

  // Check if app uses dynamic JSON body
  const usesDynamicBody = app?.useDynamicJsonBody ?? false;

  // Load app details
  useEffect(() => {
    const loadApp = async () => {
      if (!nodeId) {
        setError("No app ID provided");
        setLoading(false);
        return;
      }

      try {
        const response = await getAppById(nodeId);
        if (response.error) {
          setError("Failed to load app details");
        } else if (response.data) {
          setApp(response.data);
        }
      } catch (_err) {
        setError("Failed to load app details");
      } finally {
        setLoading(false);
      }
    };

    loadApp();
  }, [nodeId]);

  useEffect(() => {
    const loadSpace = async () => {
      if (!app?.folderId) {
        setError("This app is not assigned to a folder");
        return;
      }

      setSpaceLoading(true);
      setSpaceId("");

      try {
        const folderResponse = await getFolderById(app.folderId);

        if (folderResponse.error) {
          setError("Failed to load space information");
          return;
        }

        const folderSpaceId = (
          folderResponse.data as { spaceId?: string | null } | undefined
        )?.spaceId;

        if (!folderSpaceId) {
          setError("Space information missing for this folder");
          return;
        }

        setSpaceId(folderSpaceId);
      } catch (_err) {
        setError("Failed to load space information");
      } finally {
        setSpaceLoading(false);
      }
    };

    if (app) {
      loadSpace();
    }
  }, [app]);

  const handleRunApp = useCallback(async () => {
    if (!nodeId) {
      return;
    }

    // Validate inputs if app has inputs
    if (hasInputs && app?.inputs) {
      const errors: Record<string, string> = {};
      for (const input of app.inputs) {
        const title = input.title || "";
        const value = inputValues[title] || "";
        const fieldType = input.type
          ? fromBackendInputType(input.type)
          : "text";

        // Check required fields
        if (input.required && !value) {
          errors[title] = "This field is required";
        }
        // Validate email format (only if there's a value)
        else if (fieldType === "email" && value && !isValidEmail(value)) {
          errors[title] = "Please enter a valid email address";
        }
      }
      if (Object.keys(errors).length > 0) {
        setFormErrors(errors);
        return;
      }
    }

    // Validate dynamic body if app uses it
    if (usesDynamicBody) {
      // Filter out empty key-value pairs
      const nonEmptyPairs = dynamicBody.filter(
        (pair) => pair.key.trim() !== "" || pair.value.trim() !== ""
      );

      // Check for empty keys (but non-empty values)
      const hasEmptyKey = nonEmptyPairs.some(
        (pair) => pair.key.trim() === "" && pair.value.trim() !== ""
      );
      if (hasEmptyKey) {
        setDynamicBodyError("All keys must have a value");
        return;
      }

      // Check for duplicate keys
      const keys = nonEmptyPairs.map((pair) => pair.key.trim()).filter(Boolean);
      const uniqueKeys = new Set(keys);
      if (keys.length !== uniqueKeys.size) {
        setDynamicBodyError("Duplicate keys are not allowed");
        return;
      }

      // Check max items
      if (nonEmptyPairs.length > 10) {
        setDynamicBodyError("Maximum 10 key-value pairs allowed");
        return;
      }
    }

    setFormErrors({});
    setDynamicBodyError(null);
    setRunning(true);
    setRunError(null);
    setResult(null);

    try {
      // Convert inputValues object to array format
      const inputValuesArray = Object.entries(inputValues).map(
        ([title, value]) => ({
          title,
          value: String(value),
        })
      );

      // Prepare dynamic body if app uses it
      const dynamicBodyForApi = usesDynamicBody
        ? dynamicBody.filter(
            (pair) => pair.key.trim() !== "" && pair.value.trim() !== ""
          )
        : undefined;

      const response = await runApp(
        nodeId,
        inputValuesArray,
        dynamicBodyForApi
      );
      if (response.error) {
        setRunError((response.error?.message as string) || "Failed to run app");
      } else {
        setResult(response.data);
      }
    } catch (err) {
      setRunError(err instanceof Error ? err.message : "Failed to run app");
    } finally {
      setRunning(false);
    }
  }, [
    nodeId,
    hasInputs,
    inputValues,
    app?.inputs,
    usesDynamicBody,
    dynamicBody,
  ]);

  // Handle input value changes
  const handleInputChange = (title: string, value: string) => {
    setInputValues((prev) => ({ ...prev, [title]: value }));
    // Clear error when user starts typing
    if (formErrors[title]) {
      setFormErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[title];
        return newErrors;
      });
    }
  };

  // Validate email on blur
  const handleEmailBlur = (title: string, value: string) => {
    if (value && !isValidEmail(value)) {
      setFormErrors((prev) => ({
        ...prev,
        [title]: "Please enter a valid email address",
      }));
    }
  };

  // Render an input field based on its type
  const renderInputField = (input: AppInput) => {
    const title = input.title || "";
    const fieldType = input.type ? fromBackendInputType(input.type) : "text";
    const value = inputValues[title] || "";
    const hasError = !!formErrors[title];

    switch (fieldType) {
      case "boolean":
        return (
          <div className="flex items-center space-x-2">
            <Switch
              id={`input-${title}`}
              checked={value === "true"}
              onCheckedChange={(checked) =>
                handleInputChange(title, checked ? "true" : "false")
              }
              disabled={running}
            />
            <Label htmlFor={`input-${title}`} className="cursor-pointer">
              {value === "true" ? "Yes" : "No"}
            </Label>
          </div>
        );
      case "email":
        return (
          <Input
            id={`input-${title}`}
            type="email"
            value={value}
            onChange={(e) => handleInputChange(title, e.target.value)}
            onBlur={(e) => handleEmailBlur(title, e.target.value)}
            placeholder={
              extractDefaultValue(input.defaultValue)
                ? `Default: ${extractDefaultValue(input.defaultValue)}`
                : `Enter ${title}`
            }
            disabled={running}
            className={hasError ? "border-red-500" : ""}
          />
        );
      case "date":
        return (
          <Input
            id={`input-${title}`}
            type="date"
            value={value}
            onChange={(e) => handleInputChange(title, e.target.value)}
            disabled={running}
            className={hasError ? "border-red-500" : ""}
          />
        );
      case "integer":
        return (
          <Input
            id={`input-${title}`}
            type="number"
            value={value}
            onChange={(e) => handleInputChange(title, e.target.value)}
            placeholder={
              extractDefaultValue(input.defaultValue)
                ? `Default: ${extractDefaultValue(input.defaultValue)}`
                : `Enter ${title}`
            }
            disabled={running}
            className={hasError ? "border-red-500" : ""}
          />
        );
      case "radio": {
        const options = input.type
          ? getRadioOptionsFromBackendType(input.type)
          : [];
        return (
          <RadioGroup
            value={value}
            onValueChange={(val) => handleInputChange(title, val)}
            disabled={running}
          >
            {options.map((opt) => (
              <div key={opt.value} className="flex items-center space-x-2">
                <RadioGroupItem
                  value={opt.value}
                  id={`input-${title}-${opt.value}`}
                />
                <Label
                  htmlFor={`input-${title}-${opt.value}`}
                  className="font-normal cursor-pointer"
                >
                  {opt.label || opt.value}
                </Label>
              </div>
            ))}
          </RadioGroup>
        );
      }
      default:
        return (
          <Input
            id={`input-${title}`}
            type="text"
            value={value}
            onChange={(e) => handleInputChange(title, e.target.value)}
            placeholder={
              extractDefaultValue(input.defaultValue)
                ? `Default: ${extractDefaultValue(input.defaultValue)}`
                : `Enter ${title}`
            }
            disabled={running}
            className={hasError ? "border-red-500" : ""}
          />
        );
    }
  };

  const handleGoBack = () => {
    if (app?.parentId) {
      navigate(`/spaces/${app.parentId}`);
    } else {
      navigate("/spaces");
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <Loader className="h-8 w-8 animate-spin mx-auto mb-4" />
          <p className="text-muted-foreground">Loading app...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <p className="text-red-500 mb-4">{error}</p>
          <Button onClick={handleGoBack}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Go Back
          </Button>
        </div>
      </div>
    );
  }

  const spaceReady = !!spaceId && !spaceLoading;

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b bg-card/30">
        <div className="container mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button variant="outline" onClick={handleGoBack}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back
              </Button>
              <div>
                <h1 className="text-2xl font-semibold">
                  {app?.name || "Run App"}
                </h1>
                <p className="text-muted-foreground">
                  Executing app and showing results
                </p>
              </div>
            </div>
            {spaceReady ? (
              <PermissionButton
                spaceId={spaceId}
                permission="run_app"
                onClick={handleRunApp}
                disabled={running}
                variant={running ? "secondary" : "default"}
              >
                {running ? (
                  <>
                    <Loader className="mr-2 h-4 w-4 animate-spin" />
                    Running...
                  </>
                ) : (
                  <>
                    <Play className="mr-2 h-4 w-4" />
                    {result ? "Run Again" : "Run"}
                  </>
                )}
              </PermissionButton>
            ) : (
              <Button variant="outline" disabled>
                <Loader className="mr-2 h-4 w-4 animate-spin" />
                Checking access...
              </Button>
            )}
          </div>
        </div>
      </header>

      <main className="container mx-auto p-6 space-y-6">
        {/* Permission Warning */}
        {spaceReady && !canRunApp && (
          <Card className="border-yellow-200 bg-yellow-50">
            <CardContent className="py-4">
              <div className="flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-yellow-600 mt-0.5" />
                <div>
                  <h4 className="font-medium text-yellow-900 mb-1">
                    Permission Required
                  </h4>
                  <p className="text-sm text-yellow-800">
                    You don't have permission to run this app.{" "}
                    <Link
                      to={`/spaces/${spaceId}/permissions`}
                      className="underline hover:no-underline"
                    >
                      Ask a moderator
                    </Link>{" "}
                    for permissions.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Run App Form - shown until first run */}
        {!result && (
          <Card>
            <CardHeader>
              <CardTitle>
                {hasInputs || usesDynamicBody ? "App Inputs" : "Run App"}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {app?.inputs.map((input, index) => (
                  <div key={input.title || index} className="space-y-2">
                    <Label htmlFor={`input-${input.title}`}>
                      {input.title}
                      {input.required && (
                        <span className="text-red-500 ml-1">*</span>
                      )}
                    </Label>
                    {renderInputField(input)}
                    {formErrors[input.title || ""] && (
                      <p className="text-sm text-red-500">
                        {formErrors[input.title || ""]}
                      </p>
                    )}
                  </div>
                ))}

                {/* Dynamic Body Editor */}
                {usesDynamicBody && (
                  <div className="space-y-2">
                    <Label>JSON Body Parameters</Label>
                    <p className="text-sm text-muted-foreground">
                      Define the key-value pairs for the request body (max 10
                      pairs)
                    </p>
                    <DynamicBodyEditor
                      items={dynamicBody}
                      onChange={(items) => {
                        setDynamicBody(items);
                        if (dynamicBodyError) {
                          setDynamicBodyError(null);
                        }
                      }}
                      disabled={running}
                    />
                    {dynamicBodyError && (
                      <p className="text-sm text-red-500">{dynamicBodyError}</p>
                    )}
                  </div>
                )}

                <div className="pt-4">
                  {spaceReady ? (
                    <PermissionButton
                      spaceId={spaceId}
                      permission="run_app"
                      onClick={handleRunApp}
                      disabled={running}
                      variant={running ? "secondary" : "default"}
                    >
                      {running ? (
                        <>
                          <Loader className="mr-2 h-4 w-4 animate-spin" />
                          Running...
                        </>
                      ) : (
                        <>
                          <Play className="mr-2 h-4 w-4" />
                          Run App
                        </>
                      )}
                    </PermissionButton>
                  ) : (
                    <Button variant="outline" disabled>
                      <Loader className="mr-2 h-4 w-4 animate-spin" />
                      Checking access...
                    </Button>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {/* App Configuration Display - only shown after run attempted */}
        {(result || running || runError) && (
          <Card>
            <CardHeader>
              <CardTitle>App Configuration</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <h4 className="font-medium mb-2">Resource</h4>
                <p className="text-sm text-muted-foreground">
                  {app?.resourceId || "No resource selected"}
                </p>
              </div>

              {app?.urlPath && (
                <div>
                  <h4 className="font-medium mb-2">URL Path</h4>
                  <p className="text-sm bg-muted p-2 rounded">{app.urlPath}</p>
                </div>
              )}

              {app?.urlParameters?.length > 0 && (
                <div>
                  <h4 className="font-medium mb-2">URL Parameters</h4>
                  <div className="space-y-1">
                    {app.urlParameters.map(
                      (param: KeyValuePair, index: number) => (
                        <div
                          key={`param-${param.key}-${index}`}
                          className="text-sm bg-muted p-2 rounded flex justify-between"
                        >
                          <span className="font-mono">{param.key}</span>
                          <span className="text-muted-foreground">
                            {param.value}
                          </span>
                        </div>
                      )
                    )}
                  </div>
                </div>
              )}

              {app?.headers?.length > 0 && (
                <div>
                  <h4 className="font-medium mb-2">Headers</h4>
                  <div className="space-y-1">
                    {app.headers.map((header: KeyValuePair, index: number) => (
                      <div
                        key={`header-${header.key}-${index}`}
                        className="text-sm bg-muted p-2 rounded flex justify-between"
                      >
                        <span className="font-mono">{header.key}</span>
                        <span className="text-muted-foreground">
                          {header.value}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {app?.body?.length > 0 && (
                <div>
                  <h4 className="font-medium mb-2">Body</h4>
                  <div className="space-y-1">
                    {app.body.map((bodyParam: KeyValuePair, index: number) => (
                      <div
                        key={`body-${bodyParam.key}-${index}`}
                        className="text-sm bg-muted p-2 rounded flex justify-between"
                      >
                        <span className="font-mono">{bodyParam.key}</span>
                        <span className="text-muted-foreground">
                          {bodyParam.value}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        )}

        {/* Execution Status - only shown after run attempted */}
        {(result || running || runError) && (
          <Card>
            <CardHeader>
              <CardTitle>Execution Status</CardTitle>
            </CardHeader>
            <CardContent>
              {running && (
                <div className="flex items-center gap-2 text-blue-600">
                  <Loader className="h-4 w-4 animate-spin" />
                  <span>Running app...</span>
                </div>
              )}

              {runError && (
                <div className="text-red-600 bg-red-50 p-3 rounded">
                  <h4 className="font-medium mb-1">Error</h4>
                  <p className="text-sm">{runError}</p>
                </div>
              )}

              {result && !running && (
                <div className="text-green-600 bg-green-50 p-3 rounded">
                  <h4 className="font-medium mb-1">Success</h4>
                  <p className="text-sm">App executed successfully</p>
                </div>
              )}
            </CardContent>
          </Card>
        )}

        {/* Request Details */}
        {result && (
          <Card>
            <CardHeader>
              <CardTitle>
                <Collapsible
                  open={!isRequestCollapsed}
                  onOpenChange={(open) => setIsRequestCollapsed(!open)}
                >
                  <CollapsibleTrigger className="flex items-center gap-2 w-full justify-start">
                    {isRequestCollapsed ? (
                      <ChevronRight className="h-4 w-4" />
                    ) : (
                      <ChevronDown className="h-4 w-4" />
                    )}
                    Request Details
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <div className="mt-4">
                      <div className="space-y-4">
                        <div>
                          <h4 className="font-medium mb-2">Method & URL</h4>
                          <p className="text-sm bg-muted p-2 rounded font-mono">
                            {result.executableRequest?.httpMethod}{" "}
                            {result.executableRequest?.baseUrl}
                            {result.executableRequest?.urlParameters &&
                              result.executableRequest.urlParameters.length >
                                0 &&
                              `?${result.executableRequest.urlParameters
                                .map(
                                  (tuple) =>
                                    `${tuple.item1 ?? ""}=${tuple.item2 ?? ""}`
                                )
                                .join("&")}`}
                          </p>
                        </div>

                        {result.executableRequest?.headers &&
                          result.executableRequest.headers.length > 0 && (
                            <div>
                              <h4 className="font-medium mb-2">Headers</h4>
                              <div className="bg-muted p-3 rounded">
                                <pre className="text-sm overflow-x-auto whitespace-pre-wrap">
                                  {JSON.stringify(
                                    Object.fromEntries(
                                      result.executableRequest.headers.map(
                                        (tuple) => [
                                          tuple.item1 ?? "",
                                          tuple.item2 ?? "",
                                        ]
                                      )
                                    ),
                                    null,
                                    2
                                  )}
                                </pre>
                              </div>
                            </div>
                          )}

                        {result.executableRequest?.body &&
                          result.executableRequest.body.length > 0 && (
                            <div>
                              <h4 className="font-medium mb-2">Body</h4>
                              <div className="bg-muted p-3 rounded">
                                <pre className="text-sm overflow-x-auto whitespace-pre-wrap">
                                  {JSON.stringify(
                                    Object.fromEntries(
                                      result.executableRequest.body.map(
                                        (tuple) => [
                                          tuple.item1 ?? "",
                                          tuple.item2 ?? "",
                                        ]
                                      )
                                    ),
                                    null,
                                    2
                                  )}
                                </pre>
                              </div>
                            </div>
                          )}

                        <div>
                          <h4 className="font-medium mb-2">Timing</h4>
                          <p className="text-sm text-muted-foreground">
                            Started:{" "}
                            {new Date(result.startedAt).toLocaleString()}
                            <br />
                            Completed:{" "}
                            {new Date(result.completedAt).toLocaleString()}
                            <br />
                            Duration:{" "}
                            {(
                              (new Date(result.completedAt).getTime() -
                                new Date(result.startedAt).getTime()) /
                              1000
                            ).toFixed(2)}
                            s
                          </p>
                        </div>
                      </div>
                    </div>
                  </CollapsibleContent>
                </Collapsible>
              </CardTitle>
            </CardHeader>
          </Card>
        )}

        {/* Response Display */}
        {result && (
          <Card>
            <CardHeader>
              <CardTitle>Response</CardTitle>
            </CardHeader>
            <CardContent>
              {result.response ? (
                (() => {
                  const { content, isJson } = formatResponse(result.response);
                  return (
                    <div className="space-y-2">
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-muted-foreground">
                          Content Type: {isJson ? "JSON" : "Text/HTML"}
                        </span>
                      </div>
                      <div className="bg-muted p-4 rounded-lg">
                        <pre className="text-sm overflow-x-auto whitespace-pre-wrap">
                          {content}
                        </pre>
                      </div>
                    </div>
                  );
                })()
              ) : (
                <div className="bg-muted p-4 rounded-lg">
                  <p className="text-sm text-muted-foreground">
                    No response data
                  </p>
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </main>
    </div>
  );
};

export default RunApp;

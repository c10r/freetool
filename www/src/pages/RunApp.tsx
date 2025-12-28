import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  ArrowLeft,
  Play,
  Loader,
  ChevronDown,
  ChevronRight,
  AlertCircle,
} from "lucide-react";
import { getAppById, getFolderById, runApp } from "@/api/api";
import { PermissionButton } from "@/components/PermissionButton";
import { useHasPermission } from "@/hooks/usePermissions";
import type { components } from "@/schema";

type AppData = components["schemas"]["AppData"];
type RunData = components["schemas"]["RunData"];
type KeyValuePair = components["schemas"]["KeyValuePair"];

// Helper function to safely format response content
const formatResponse = (
  response: string,
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
  const [workspaceId, setWorkspaceId] = useState("");
  const [workspaceLoading, setWorkspaceLoading] = useState(false);

  const canRunApp = useHasPermission(workspaceId, "run_app");

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
      } catch (err) {
        setError("Failed to load app details");
      } finally {
        setLoading(false);
      }
    };

    loadApp();
  }, [nodeId]);

  useEffect(() => {
    const loadWorkspace = async () => {
      if (!app?.folderId) {
        setError("This app is not assigned to a folder");
        return;
      }

      setWorkspaceLoading(true);
      setWorkspaceId("");

      try {
        const folderResponse = await getFolderById(app.folderId);

        if (folderResponse.error) {
          setError("Failed to load workspace information");
          return;
        }

        const folderWorkspaceId = (
          folderResponse.data as { workspaceId?: string | null } | undefined
        )?.workspaceId;

        if (!folderWorkspaceId) {
          setError("Workspace information missing for this folder");
          return;
        }

        setWorkspaceId(folderWorkspaceId);
      } catch (err) {
        setError("Failed to load workspace information");
      } finally {
        setWorkspaceLoading(false);
      }
    };

    if (app) {
      loadWorkspace();
    }
  }, [app]);

  const handleRunApp = useCallback(async () => {
    if (!nodeId) return;

    setRunning(true);
    setRunError(null);
    setResult(null);

    try {
      const response = await runApp(nodeId);
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
  }, [nodeId]);

  // Auto-run the app when component loads
  useEffect(() => {
    if (app && !running && !result && !runError) {
      handleRunApp();
    }
  }, [app, running, result, runError, handleRunApp]);

  const handleGoBack = () => {
    if (app?.parentId) {
      navigate(`/workspaces/${app.parentId}`);
    } else {
      navigate("/workspaces");
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

  const workspaceReady = !!workspaceId && !workspaceLoading;

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
            {workspaceReady ? (
              <PermissionButton
                workspaceId={workspaceId}
                permission="run_app"
                tooltipMessage="You don't have permission to run this app. Contact your team admin."
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
                    Run Again
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
        {workspaceReady && !canRunApp && (
          <Card className="border-yellow-200 bg-yellow-50">
            <CardContent className="py-4">
              <div className="flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-yellow-600 mt-0.5" />
                <div>
                  <h4 className="font-medium text-yellow-900 mb-1">
                    Permission Required
                  </h4>
                  <p className="text-sm text-yellow-800">
                    You don't have permission to run this app. Contact your team
                    admin to request run_app access.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {/* App Configuration Display */}
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
                  {app.urlParameters.map((param: KeyValuePair, index: number) => (
                    <div
                      key={index}
                      className="text-sm bg-muted p-2 rounded flex justify-between"
                    >
                      <span className="font-mono">{param.key}</span>
                      <span className="text-muted-foreground">
                        {param.value}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {app?.headers?.length > 0 && (
              <div>
                <h4 className="font-medium mb-2">Headers</h4>
                <div className="space-y-1">
                  {app.headers.map((header: KeyValuePair, index: number) => (
                    <div
                      key={index}
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
                      key={index}
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

        {/* Execution Status */}
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
                            {result.executableRequest?.value?.httpMethod}{" "}
                            {result.executableRequest?.value?.baseUrl}
                            {result.executableRequest?.value?.urlParameters &&
                              result.executableRequest.value.urlParameters.length >
                                0 &&
                              `?${result.executableRequest.value.urlParameters
                                .map((tuple) => `${tuple.item1 ?? ""}=${tuple.item2 ?? ""}`)
                                .join("&")}`}
                          </p>
                        </div>

                        {result.executableRequest?.value?.headers &&
                          result.executableRequest.value.headers.length > 0 && (
                            <div>
                              <h4 className="font-medium mb-2">Headers</h4>
                              <div className="bg-muted p-3 rounded">
                                <pre className="text-sm overflow-x-auto whitespace-pre-wrap">
                                  {JSON.stringify(
                                    Object.fromEntries(
                                      result.executableRequest.value.headers.map(
                                        (tuple) => [
                                          tuple.item1 ?? "",
                                          tuple.item2 ?? "",
                                        ]
                                      )
                                    ),
                                    null,
                                    2,
                                  )}
                                </pre>
                              </div>
                            </div>
                          )}

                        {result.executableRequest?.value?.body &&
                          result.executableRequest.value.body.length > 0 && (
                            <div>
                              <h4 className="font-medium mb-2">Body</h4>
                              <div className="bg-muted p-3 rounded">
                                <pre className="text-sm overflow-x-auto whitespace-pre-wrap">
                                  {JSON.stringify(
                                    Object.fromEntries(
                                      result.executableRequest.value.body.map(
                                        (tuple) => [
                                          tuple.item1 ?? "",
                                          tuple.item2 ?? "",
                                        ]
                                      )
                                    ),
                                    null,
                                    2,
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

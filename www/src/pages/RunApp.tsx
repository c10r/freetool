import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ArrowLeft, Play, Loader } from "lucide-react";
import { getAppById, runApp } from "@/api/api";

const RunApp = () => {
  const { nodeId } = useParams<{ nodeId: string }>();
  const navigate = useNavigate();

  const [app, setApp] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [runError, setRunError] = useState<string | null>(null);

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

  // Auto-run the app when component loads
  useEffect(() => {
    if (app && !running && !result && !runError) {
      handleRunApp();
    }
  }, [app]);

  const handleRunApp = async () => {
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
  };

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
            <Button
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
            </Button>
          </div>
        </div>
      </header>

      <main className="container mx-auto p-6 space-y-6">
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
                  {app.urlParameters.map((param: any, index: number) => (
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
                  {app.headers.map((header: any, index: number) => (
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
                  {app.body.map((bodyParam: any, index: number) => (
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

        {/* Results Display */}
        {result && (
          <Card>
            <CardHeader>
              <CardTitle>Results</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="bg-muted p-4 rounded-lg">
                <pre className="text-sm overflow-x-auto whitespace-pre-wrap">
                  {JSON.stringify(result, null, 2)}
                </pre>
              </div>
            </CardContent>
          </Card>
        )}
      </main>
    </div>
  );
};

export default RunApp;

import { AlertCircle, ArrowLeft, Loader2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getDashboardById, getFolderById } from "@/api/api";
import { Button } from "@/components/ui/button";
import DashboardView from "@/features/space/components/DashboardView";
import type { DashboardNode } from "@/features/space/types";

const mapToDashboardNode = (
  dashboardData: Awaited<ReturnType<typeof getDashboardById>>["data"]
): DashboardNode | null => {
  if (!dashboardData?.id) {
    return null;
  }

  return {
    id: dashboardData.id,
    name: dashboardData.name || "Unnamed Dashboard",
    type: "dashboard",
    parentId: dashboardData.folderId || undefined,
    prepareAppId: dashboardData.prepareAppId ?? undefined,
    configuration: dashboardData.configuration || "",
  };
};

export default function RunDashboard() {
  const { nodeId, spaceId: routeSpaceId } = useParams<{
    nodeId: string;
    spaceId: string;
  }>();

  const [dashboard, setDashboard] = useState<DashboardNode | null>(null);
  const [spaceId, setSpaceId] = useState(routeSpaceId || "");
  const [folderName, setFolderName] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadDashboard = async () => {
      if (!nodeId) {
        setError("No dashboard ID provided");
        setLoading(false);
        return;
      }

      try {
        const dashboardResponse = await getDashboardById(nodeId);

        if (dashboardResponse.error || !dashboardResponse.data) {
          setError("Failed to load dashboard details");
          return;
        }

        const mappedDashboard = mapToDashboardNode(dashboardResponse.data);

        if (!mappedDashboard) {
          setError("Invalid dashboard data");
          return;
        }

        setDashboard(mappedDashboard);

        const folderId = dashboardResponse.data.folderId;
        if (folderId) {
          const folderResponse = await getFolderById(folderId);

          if (!folderResponse.error) {
            const resolvedFolderName = (
              folderResponse.data as { name?: string | null } | undefined
            )?.name;
            const folderSpaceId = (
              folderResponse.data as { spaceId?: string | null } | undefined
            )?.spaceId;

            setFolderName(resolvedFolderName || "");
            if (folderSpaceId) {
              setSpaceId(folderSpaceId);
            }
          }
        }
      } catch {
        setError("Failed to load dashboard details");
      } finally {
        setLoading(false);
      }
    };

    loadDashboard();
  }, [nodeId]);

  if (loading) {
    return (
      <div className="min-h-screen bg-background p-6 flex items-center justify-center">
        <div className="flex items-center gap-3 text-muted-foreground">
          <Loader2 className="h-5 w-5 animate-spin" />
          <span>Loading dashboard...</span>
        </div>
      </div>
    );
  }

  if (error || !dashboard) {
    return (
      <div className="min-h-screen bg-background p-6 space-y-4">
        <Link to={spaceId ? `/spaces/${spaceId}` : "/spaces"}>
          <Button variant="outline" size="sm" className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back
          </Button>
        </Link>
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-red-700 flex items-center gap-2">
          <AlertCircle className="h-4 w-4" />
          <span>{error || "Dashboard not found"}</span>
        </div>
      </div>
    );
  }

  return (
    <DashboardView
      dashboard={dashboard}
      nodes={{}}
      spaceId={spaceId}
      mode="run"
      runFolderName={folderName}
    />
  );
}

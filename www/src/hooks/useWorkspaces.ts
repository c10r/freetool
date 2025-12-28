import { useQuery } from "@tanstack/react-query";
import { getGroupById, getWorkspaces } from "@/api/api";
import type {
  Workspace,
  WorkspaceListResponse,
  WorkspaceWithGroup,
} from "@/types/workspace";

interface WorkspaceQueryResult {
  workspaces: WorkspaceWithGroup[];
  total: number;
}

/**
 * Hook to fetch all workspaces the current user has access to
 *
 * @returns Workspace list data, loading state, and error state
 */
export function useWorkspaces() {
  const { data, isLoading, error, refetch } = useQuery<WorkspaceQueryResult>({
    queryKey: ["workspaces"],
    queryFn: async () => {
      const result = await getWorkspaces();

      if (result.error || !result.data) {
        throw new Error(result.error?.message || "Failed to fetch workspaces");
      }

      const responseData = result.data as WorkspaceListResponse | Workspace[];
      const workspaceItems: Workspace[] = Array.isArray(responseData)
        ? responseData
        : responseData.items || [];

      const uniqueGroupIds = Array.from(
        new Set(workspaceItems.map((workspace) => workspace.groupId))
      );
      const groupNameMap = new Map<string, string>();

      await Promise.all(
        uniqueGroupIds.map(async (groupId) => {
          try {
            const groupResult = await getGroupById(groupId);
            const groupName = groupResult.data?.name;

            if (groupName) {
              groupNameMap.set(groupId, groupName);
            }
          } catch {
            // Ignore lookup failures and fall back to workspace ID
          }
        })
      );

      const workspaces: WorkspaceWithGroup[] = workspaceItems.map(
        (workspace) => ({
          ...workspace,
          groupName: groupNameMap.get(workspace.groupId) || workspace.groupId,
        })
      );

      const total = Array.isArray(responseData)
        ? workspaces.length
        : (responseData.total ?? workspaces.length);

      return {
        workspaces,
        total,
      };
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  return {
    workspaces: data?.workspaces || [],
    total: data?.total || 0,
    isLoading,
    error,
    refetch,
  };
}

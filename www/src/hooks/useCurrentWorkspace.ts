import { useQuery } from "@tanstack/react-query";
import { getWorkspaceById } from "@/api/api";
import type { Workspace } from "@/types/workspace";

/**
 * Hook to fetch a specific workspace by ID
 *
 * @param workspaceId - The ID of the workspace to fetch
 * @returns Workspace data, loading state, and error state
 */
export function useCurrentWorkspace(workspaceId: string | undefined) {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["workspace", workspaceId],
    queryFn: async () => {
      if (!workspaceId) {
        return null;
      }
      const result = await getWorkspaceById(workspaceId);
      if (result.error) {
        throw new Error(result.error.message || "Failed to fetch workspace");
      }
      return result.data as Workspace;
    },
    enabled: !!workspaceId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  return {
    workspace: data || null,
    isLoading,
    error,
    refetch,
  };
}

/**
 * useSpaceMembersPermissions Hook
 *
 * Provides access to space members and their permissions with TanStack Query
 * for caching and automatic refetching.
 */

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getSpaceMembersPermissions, updateUserPermissions } from "@/api/api";
import type {
  SpaceMemberPermissions,
  SpacePermissions,
} from "@/types/permissions";

/**
 * Converts backend permission format (camelCase) to frontend format (snake_case)
 */
function mapPermissionsFromApi(
  apiPermissions:
    | {
        createResource?: boolean;
        editResource?: boolean;
        deleteResource?: boolean;
        createApp?: boolean;
        editApp?: boolean;
        deleteApp?: boolean;
        runApp?: boolean;
        createFolder?: boolean;
        editFolder?: boolean;
        deleteFolder?: boolean;
        createDashboard?: boolean;
        editDashboard?: boolean;
        deleteDashboard?: boolean;
        runDashboard?: boolean;
      }
    | null
    | undefined
): SpacePermissions {
  return {
    create_resource: apiPermissions?.createResource ?? false,
    edit_resource: apiPermissions?.editResource ?? false,
    delete_resource: apiPermissions?.deleteResource ?? false,
    create_app: apiPermissions?.createApp ?? false,
    edit_app: apiPermissions?.editApp ?? false,
    delete_app: apiPermissions?.deleteApp ?? false,
    run_app: apiPermissions?.runApp ?? false,
    create_folder: apiPermissions?.createFolder ?? false,
    edit_folder: apiPermissions?.editFolder ?? false,
    delete_folder: apiPermissions?.deleteFolder ?? false,
    create_dashboard: apiPermissions?.createDashboard ?? false,
    edit_dashboard: apiPermissions?.editDashboard ?? false,
    delete_dashboard: apiPermissions?.deleteDashboard ?? false,
    run_dashboard: apiPermissions?.runDashboard ?? false,
  };
}

/**
 * Converts frontend permission format (snake_case) to backend format (camelCase)
 */
function mapPermissionsToApi(permissions: SpacePermissions): {
  createResource: boolean;
  editResource: boolean;
  deleteResource: boolean;
  createApp: boolean;
  editApp: boolean;
  deleteApp: boolean;
  runApp: boolean;
  createFolder: boolean;
  editFolder: boolean;
  deleteFolder: boolean;
  createDashboard: boolean;
  editDashboard: boolean;
  deleteDashboard: boolean;
  runDashboard: boolean;
} {
  return {
    createResource: permissions.create_resource,
    editResource: permissions.edit_resource,
    deleteResource: permissions.delete_resource,
    createApp: permissions.create_app,
    editApp: permissions.edit_app,
    deleteApp: permissions.delete_app,
    runApp: permissions.run_app,
    createFolder: permissions.create_folder,
    editFolder: permissions.edit_folder,
    deleteFolder: permissions.delete_folder,
    createDashboard: permissions.create_dashboard,
    editDashboard: permissions.edit_dashboard,
    deleteDashboard: permissions.delete_dashboard,
    runDashboard: permissions.run_dashboard,
  };
}

/**
 * Cache time-to-live for permissions (5 minutes)
 */
const PERMISSION_CACHE_TTL = 5 * 60 * 1000;

interface UseSpaceMembersPermissionsOptions {
  skip?: number;
  take?: number;
}

interface UseSpaceMembersPermissionsResult {
  members: SpaceMemberPermissions[];
  spaceName: string;
  totalCount: number;
  isLoading: boolean;
  error: Error | null;
  refetch: () => void;
  updatePermissions: (
    userId: string,
    permissions: SpacePermissions
  ) => Promise<void>;
  isUpdating: boolean;
}

/**
 * Hook to fetch all members and their permissions for a space
 *
 * @param spaceId - The ID of the space to fetch permissions for
 * @param options - Optional pagination parameters (skip, take)
 * @returns Object containing members, loading state, and update function
 *
 * @example
 * ```tsx
 * function PermissionsView({ spaceId }) {
 *   const { members, isLoading, updatePermissions, totalCount } = useSpaceMembersPermissions(spaceId, { skip: 0, take: 50 });
 *
 *   if (isLoading) return <Skeleton />;
 *
 *   return (
 *     <ul>
 *       {members.map(member => (
 *         <li key={member.userId}>{member.userName}</li>
 *       ))}
 *     </ul>
 *   );
 * }
 * ```
 */
export function useSpaceMembersPermissions(
  spaceId: string,
  options?: UseSpaceMembersPermissionsOptions
): UseSpaceMembersPermissionsResult {
  const queryClient = useQueryClient();
  const { skip, take } = options ?? {};

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["spaceMembersPermissions", spaceId, skip, take],
    queryFn: async () => {
      const result = await getSpaceMembersPermissions(spaceId, skip, take);
      if (result.error || !result.data) {
        throw new Error(
          result.error?.message || "Failed to fetch members permissions"
        );
      }
      // Map the API response to frontend format
      // members is now a PagedResult with items and totalCount
      const membersData = result.data.members;
      const items = membersData?.items ?? [];
      const totalCount = membersData?.totalCount ?? 0;

      const members: SpaceMemberPermissions[] = items.map((member) => ({
        userId: member.userId ?? "",
        userName: member.userName ?? "",
        userEmail: member.userEmail ?? "",
        profilePicUrl: member.profilePicUrl ?? undefined,
        isModerator: member.isModerator ?? false,
        isOrgAdmin: member.isOrgAdmin ?? false,
        permissions: mapPermissionsFromApi(member.permissions),
      }));
      return {
        spaceId: result.data.spaceId ?? "",
        spaceName: result.data.spaceName ?? "",
        members,
        totalCount,
      };
    },
    staleTime: PERMISSION_CACHE_TTL,
    retry: 2,
    enabled: !!spaceId,
  });

  const mutation = useMutation({
    mutationFn: async ({
      userId,
      permissions,
    }: {
      userId: string;
      permissions: SpacePermissions;
    }) => {
      // Convert frontend format to backend format
      const apiPermissions = mapPermissionsToApi(permissions);
      const result = await updateUserPermissions({
        spaceId,
        userId,
        permissions: apiPermissions as unknown as SpacePermissions,
      });
      if (result.error) {
        throw new Error(result.error.message);
      }
      return result.data;
    },
    onSuccess: () => {
      // Invalidate the query to refetch fresh data
      queryClient.invalidateQueries({
        queryKey: ["spaceMembersPermissions", spaceId],
      });
    },
  });

  const updatePermissions = async (
    userId: string,
    permissions: SpacePermissions
  ): Promise<void> => {
    await mutation.mutateAsync({ userId, permissions });
  };

  return {
    members: data?.members || [],
    spaceName: data?.spaceName || "",
    totalCount: data?.totalCount ?? 0,
    isLoading,
    error: error as Error | null,
    refetch,
    updatePermissions,
    isUpdating: mutation.isPending,
  };
}

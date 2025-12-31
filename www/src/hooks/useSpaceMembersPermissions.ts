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
  };
}

/**
 * Cache time-to-live for permissions (5 minutes)
 */
const PERMISSION_CACHE_TTL = 5 * 60 * 1000;

interface UseSpaceMembersPermissionsResult {
  members: SpaceMemberPermissions[];
  spaceName: string;
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
 * @returns Object containing members, loading state, and update function
 *
 * @example
 * ```tsx
 * function PermissionsView({ spaceId }) {
 *   const { members, isLoading, updatePermissions } = useSpaceMembersPermissions(spaceId);
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
  spaceId: string
): UseSpaceMembersPermissionsResult {
  const queryClient = useQueryClient();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["spaceMembersPermissions", spaceId],
    queryFn: async () => {
      const result = await getSpaceMembersPermissions(spaceId);
      if (result.error || !result.data) {
        throw new Error(
          result.error?.message || "Failed to fetch members permissions"
        );
      }
      // Map the API response to frontend format
      const members: SpaceMemberPermissions[] = (result.data.members ?? []).map(
        (member) => ({
          userId: member.userId ?? "",
          userName: member.userName ?? "",
          userEmail: member.userEmail ?? "",
          profilePicUrl: member.profilePicUrl ?? undefined,
          isModerator: member.isModerator ?? false,
          permissions: mapPermissionsFromApi(member.permissions),
        })
      );
      return {
        spaceId: result.data.spaceId ?? "",
        spaceName: result.data.spaceName ?? "",
        members,
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
    isLoading,
    error: error as Error | null,
    refetch,
    updatePermissions,
    isUpdating: mutation.isPending,
  };
}

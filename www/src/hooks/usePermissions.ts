/**
 * Permission hooks for checking user permissions
 *
 * These hooks provide convenient access to the authorization system
 * and handle loading states, caching, and error handling.
 */

import { useQuery } from "@tanstack/react-query";
import { getWorkspacePermissions } from "@/api/api";
import { useAuthorization } from "@/hooks/useAuthorization";
import type { Permission } from "@/types/permissions";

/**
 * Cache time-to-live for permissions (5 minutes)
 */
const PERMISSION_CACHE_TTL = 5 * 60 * 1000;

/**
 * Hook to fetch and access workspace permissions
 *
 * Automatically fetches permissions for the specified workspace
 * and returns the permissions object along with loading/error states.
 *
 * @param workspaceId - The ID of the workspace to fetch permissions for
 * @returns Object containing permissions, loading state, and helper function
 *
 * @example
 * ```tsx
 * function WorkspaceComponent({ workspaceId }) {
 *   const { permissions, isLoading, can } = useWorkspacePermissions(workspaceId);
 *
 *   if (isLoading) return <Skeleton />;
 *
 *   return (
 *     <div>
 *       {can('create_app') && <CreateAppButton />}
 *     </div>
 *   );
 * }
 * ```
 */
export function useWorkspacePermissions(workspaceId: string) {
  const {
    data: workspaceData,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ["workspacePermissions", workspaceId],
    queryFn: async () => {
      const result = await getWorkspacePermissions(workspaceId);
      if (result.error) {
        throw new Error(result.error.message);
      }
      return result.data!;
    },
    staleTime: PERMISSION_CACHE_TTL,
    retry: 2,
    enabled: !!workspaceId,
  });

  const permissions = workspaceData?.permissions || null;

  /**
   * Helper function to check if user has a specific permission
   */
  const can = (permission: Permission): boolean => {
    if (!permissions) {
      return false;
    }
    return permissions[permission] ?? false;
  };

  return {
    permissions,
    isLoading,
    error,
    refetch,
    can,
  };
}

/**
 * Hook to check if user has a specific permission on a workspace
 *
 * Returns a boolean indicating whether the user has the permission.
 * Automatically fetches permissions if not already cached.
 *
 * @param workspaceId - The ID of the workspace to check
 * @param permission - The permission to check for
 * @returns Boolean indicating if user has the permission
 *
 * @example
 * ```tsx
 * function DeleteButton({ workspaceId, appId }) {
 *   const canDelete = useHasPermission(workspaceId, 'delete_app');
 *
 *   return (
 *     <Button disabled={!canDelete} onClick={handleDelete}>
 *       Delete App
 *     </Button>
 *   );
 * }
 * ```
 */
export function useHasPermission(
  workspaceId: string,
  permission: Permission
): boolean {
  const { can } = useWorkspacePermissions(workspaceId);
  return can(permission);
}

/**
 * Hook to access current user information
 *
 * @returns Object containing current user data and loading state
 *
 * @example
 * ```tsx
 * function UserProfile() {
 *   const { currentUser, isLoading } = useCurrentUser();
 *
 *   if (isLoading) return <Skeleton />;
 *
 *   return <div>Welcome, {currentUser?.name}</div>;
 * }
 * ```
 */
export function useCurrentUser() {
  const { currentUser, isLoadingUser, userError } = useAuthorization();

  return {
    currentUser,
    isLoading: isLoadingUser,
    error: userError,
  };
}

/**
 * Hook to check if current user is an organization admin
 *
 * @returns Boolean indicating if user is an org admin
 *
 * @example
 * ```tsx
 * function AdminPanel() {
 *   const isOrgAdmin = useIsOrgAdmin();
 *
 *   if (!isOrgAdmin) {
 *     return <AccessDenied />;
 *   }
 *
 *   return <AdminSettings />;
 * }
 * ```
 */
export function useIsOrgAdmin(): boolean {
  const { isOrgAdmin } = useAuthorization();
  return isOrgAdmin();
}

/**
 * Hook to check if current user is an admin of a specific team
 *
 * @param teamId - The ID of the team to check admin status for
 * @returns Boolean indicating if user is an admin of the specified team
 *
 * @example
 * ```tsx
 * function TeamSettings({ teamId }) {
 *   const isAdmin = useIsTeamAdmin(teamId);
 *
 *   return (
 *     <div>
 *       {isAdmin && <ManageTeamButton />}
 *     </div>
 *   );
 * }
 * ```
 */
export function useIsTeamAdmin(teamId: string): boolean {
  const { isTeamAdmin } = useAuthorization();
  return isTeamAdmin(teamId);
}

/**
 * Hook to check if user has any permission on a workspace
 *
 * Useful for determining if a user should have any access to a workspace at all.
 *
 * @param workspaceId - The ID of the workspace to check
 * @returns Boolean indicating if user has at least one permission
 *
 * @example
 * ```tsx
 * function WorkspaceAccess({ workspaceId }) {
 *   const hasAccess = useHasAnyPermission(workspaceId);
 *
 *   if (!hasAccess) {
 *     return <NoAccessMessage />;
 *   }
 *
 *   return <WorkspaceContent />;
 * }
 * ```
 */
export function useHasAnyPermission(workspaceId: string): boolean {
  const { permissions, isLoading } = useWorkspacePermissions(workspaceId);

  if (isLoading || !permissions) {
    return false;
  }

  return Object.values(permissions).some((hasPermission) => hasPermission);
}

/**
 * Hook to get all permissions the user has on a workspace
 *
 * Returns an array of permission names that the user has.
 *
 * @param workspaceId - The ID of the workspace to check
 * @returns Array of permission names the user has
 *
 * @example
 * ```tsx
 * function PermissionsList({ workspaceId }) {
 *   const permissions = useUserPermissions(workspaceId);
 *
 *   return (
 *     <ul>
 *       {permissions.map(p => <li key={p}>{p}</li>)}
 *     </ul>
 *   );
 * }
 * ```
 */
export function useUserPermissions(workspaceId: string): Permission[] {
  const { permissions } = useWorkspacePermissions(workspaceId);

  if (!permissions) {
    return [];
  }

  return (Object.entries(permissions) as [Permission, boolean][])
    .filter(([, hasPermission]) => hasPermission)
    .map(([permission]) => permission);
}

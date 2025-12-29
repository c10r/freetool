/**
 * Permission hooks for checking user permissions
 *
 * These hooks provide convenient access to the authorization system
 * and handle loading states, caching, and error handling.
 */

import { useQuery } from "@tanstack/react-query";
import { getSpacePermissions } from "@/api/api";
import { useAuthorization } from "@/hooks/useAuthorization";
import type { Permission, SpacePermissions } from "@/types/permissions";

/**
 * Cache time-to-live for permissions (5 minutes)
 */
const PERMISSION_CACHE_TTL = 5 * 60 * 1000;

/**
 * Hook to fetch and access space permissions
 *
 * Automatically fetches permissions for the specified space
 * and returns the permissions object along with loading/error states.
 *
 * @param spaceId - The ID of the space to fetch permissions for
 * @returns Object containing permissions, loading state, and helper function
 *
 * @example
 * ```tsx
 * function SpaceComponent({ spaceId }) {
 *   const { permissions, isLoading, can } = useSpacePermissions(spaceId);
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
export function useSpacePermissions(spaceId: string) {
  const {
    data: spaceData,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ["spacePermissions", spaceId],
    queryFn: async () => {
      const result = await getSpacePermissions(spaceId);
      if (result.error || !result.data) {
        throw new Error(result.error?.message || "Failed to fetch permissions");
      }
      return result.data;
    },
    staleTime: PERMISSION_CACHE_TTL,
    retry: 2,
    enabled: !!spaceId,
  });

  const permissions: SpacePermissions | null = spaceData?.permissions || null;

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
 * Hook to check if user has a specific permission on a space
 *
 * Returns a boolean indicating whether the user has the permission.
 * Automatically fetches permissions if not already cached.
 *
 * @param spaceId - The ID of the space to check
 * @param permission - The permission to check for
 * @returns Boolean indicating if user has the permission
 *
 * @example
 * ```tsx
 * function DeleteButton({ spaceId, appId }) {
 *   const canDelete = useHasPermission(spaceId, 'delete_app');
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
  spaceId: string,
  permission: Permission
): boolean {
  const { can } = useSpacePermissions(spaceId);
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
 * Hook to check if current user is a moderator of a specific space
 *
 * @param spaceId - The ID of the space to check moderator status for
 * @returns Boolean indicating if user is a moderator of the specified space
 *
 * @example
 * ```tsx
 * function SpaceSettings({ spaceId }) {
 *   const isModerator = useIsSpaceModerator(spaceId);
 *
 *   return (
 *     <div>
 *       {isModerator && <ManageSpaceButton />}
 *     </div>
 *   );
 * }
 * ```
 */
export function useIsSpaceModerator(spaceId: string): boolean {
  const { isSpaceModerator } = useAuthorization();
  return isSpaceModerator(spaceId);
}

// Alias for backwards compatibility
export function useIsTeamAdmin(teamId: string): boolean {
  return useIsSpaceModerator(teamId);
}

/**
 * Hook to check if user has any permission on a space
 *
 * Useful for determining if a user should have any access to a space at all.
 *
 * @param spaceId - The ID of the space to check
 * @returns Boolean indicating if user has at least one permission
 *
 * @example
 * ```tsx
 * function SpaceAccess({ spaceId }) {
 *   const hasAccess = useHasAnyPermission(spaceId);
 *
 *   if (!hasAccess) {
 *     return <NoAccessMessage />;
 *   }
 *
 *   return <SpaceContent />;
 * }
 * ```
 */
export function useHasAnyPermission(spaceId: string): boolean {
  const { permissions, isLoading } = useSpacePermissions(spaceId);

  if (isLoading || !permissions) {
    return false;
  }

  return Object.values(permissions).some((hasPermission) => hasPermission);
}

/**
 * Hook to get all permissions the user has on a space
 *
 * Returns an array of permission names that the user has.
 *
 * @param spaceId - The ID of the space to check
 * @returns Array of permission names the user has
 *
 * @example
 * ```tsx
 * function PermissionsList({ spaceId }) {
 *   const permissions = useUserPermissions(spaceId);
 *
 *   return (
 *     <ul>
 *       {permissions.map(p => <li key={p}>{p}</li>)}
 *     </ul>
 *   );
 * }
 * ```
 */
export function useUserPermissions(spaceId: string): Permission[] {
  const { permissions } = useSpacePermissions(spaceId);

  if (!permissions) {
    return [];
  }

  return (Object.entries(permissions) as [Permission, boolean][])
    .filter(([, hasPermission]) => hasPermission)
    .map(([permission]) => permission);
}

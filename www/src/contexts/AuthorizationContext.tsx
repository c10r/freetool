/**
 * Authorization Provider
 *
 * Provides centralized permission management for the application.
 * Fetches and caches user permissions using TanStack Query.
 */

import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { getCurrentUser, getWorkspacePermissions } from "@/api/api";
import type { Permission, WorkspacePermissions } from "@/types/permissions";
import {
  AuthorizationContext,
  type AuthorizationContextValue,
} from "./authorization.context";

/**
 * Props for the AuthorizationProvider
 */
interface AuthorizationProviderProps {
  children: ReactNode;
}

/**
 * Cache time-to-live for permissions (5 minutes)
 */
const PERMISSION_CACHE_TTL = 5 * 60 * 1000;

/**
 * Provider component that manages authorization state
 *
 * @example
 * ```tsx
 * <AuthorizationProvider>
 *   <App />
 * </AuthorizationProvider>
 * ```
 */
export function AuthorizationProvider({
  children,
}: AuthorizationProviderProps) {
  const queryClient = useQueryClient();

  // Fetch current user on mount
  const {
    data: currentUserData,
    isLoading: isLoadingUser,
    error: userError,
    refetch: refetchUser,
  } = useQuery({
    queryKey: ["currentUser"],
    queryFn: async () => {
      const result = await getCurrentUser();
      if (result.error) {
        throw new Error(result.error.message);
      }
      if (!result.data) {
        throw new Error("No user data returned");
      }
      return result.data;
    },
    staleTime: PERMISSION_CACHE_TTL,
    retry: 2,
  });

  const currentUser = currentUserData || null;

  /**
   * Trigger fetching of workspace permissions
   * Uses TanStack Query's prefetch to populate the cache
   */
  const fetchWorkspacePermissions = (workspaceId: string) => {
    queryClient.prefetchQuery({
      queryKey: ["workspacePermissions", workspaceId],
      queryFn: async () => {
        const result = await getWorkspacePermissions(workspaceId);
        if (result.error) {
          throw new Error(result.error.message);
        }
        if (!result.data) {
          throw new Error("No workspace permissions data returned");
        }
        return result.data;
      },
      staleTime: PERMISSION_CACHE_TTL,
    });
  };

  /**
   * Get cached workspace permissions
   */
  const getWorkspacePermissionsData = (
    workspaceId: string
  ): WorkspacePermissions | null => {
    const data = queryClient.getQueryData<{
      permissions: WorkspacePermissions;
    }>(["workspacePermissions", workspaceId]);
    return data?.permissions || null;
  };

  /**
   * Check if user has a specific permission on a workspace
   */
  const hasPermission = (
    workspaceId: string,
    permission: Permission
  ): boolean => {
    const permissions = getWorkspacePermissionsData(workspaceId);
    if (!permissions) {
      return false;
    }
    return permissions[permission] ?? false;
  };

  /**
   * Check if current user is an organization admin
   */
  const isOrgAdmin = (): boolean => {
    return currentUser?.isOrgAdmin ?? false;
  };

  /**
   * Check if current user is an admin of a specific team
   */
  const isTeamAdmin = (teamId: string): boolean => {
    if (!currentUser) {
      return false;
    }
    return (
      currentUser.teams.some(
        (team) => team.id === teamId && team.role === "admin"
      ) ?? false
    );
  };

  /**
   * Refetch workspace permissions (invalidate cache and refetch)
   */
  const refetchWorkspacePermissions = (workspaceId: string) => {
    queryClient.invalidateQueries({
      queryKey: ["workspacePermissions", workspaceId],
    });
  };

  const value: AuthorizationContextValue = {
    currentUser,
    isLoadingUser,
    userError: userError as Error | null,
    fetchWorkspacePermissions,
    getWorkspacePermissions: getWorkspacePermissionsData,
    hasPermission,
    isOrgAdmin,
    isTeamAdmin,
    refetchUser,
    refetchWorkspacePermissions,
  };

  return (
    <AuthorizationContext.Provider value={value}>
      {children}
    </AuthorizationContext.Provider>
  );
}

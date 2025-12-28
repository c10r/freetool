/**
 * Authorization Context
 *
 * Provides centralized permission management for the application.
 * Fetches and caches user permissions using TanStack Query.
 */

import React, { createContext, useContext, ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type {
  CurrentUser,
  WorkspacePermissions,
  Permission,
} from "@/types/permissions";
import { getCurrentUser, getWorkspacePermissions } from "@/api/api";

/**
 * Shape of the authorization context value
 */
interface AuthorizationContextValue {
  currentUser: CurrentUser | null;
  isLoadingUser: boolean;
  userError: Error | null;
  fetchWorkspacePermissions: (workspaceId: string) => void;
  getWorkspacePermissions: (workspaceId: string) => WorkspacePermissions | null;
  hasPermission: (workspaceId: string, permission: Permission) => boolean;
  isOrgAdmin: () => boolean;
  isTeamAdmin: (teamId: string) => boolean;
  refetchUser: () => void;
  refetchWorkspacePermissions: (workspaceId: string) => void;
}

const AuthorizationContext = createContext<
  AuthorizationContextValue | undefined
>(undefined);

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
      return result.data!;
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
        return result.data!;
      },
      staleTime: PERMISSION_CACHE_TTL,
    });
  };

  /**
   * Get cached workspace permissions
   */
  const getWorkspacePermissionsData = (
    workspaceId: string,
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
    permission: Permission,
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
    if (!currentUser) return false;
    return (
      currentUser.teams.some(
        (team) => team.id === teamId && team.role === "admin",
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

/**
 * Hook to access the authorization context
 *
 * @throws Error if used outside of AuthorizationProvider
 *
 * @example
 * ```tsx
 * function MyComponent() {
 *   const { currentUser, hasPermission } = useAuthorization();
 *
 *   if (hasPermission('workspace-1', 'create_app')) {
 *     // Show create button
 *   }
 * }
 * ```
 */
export function useAuthorization(): AuthorizationContextValue {
  const context = useContext(AuthorizationContext);
  if (!context) {
    throw new Error(
      "useAuthorization must be used within an AuthorizationProvider",
    );
  }
  return context;
}

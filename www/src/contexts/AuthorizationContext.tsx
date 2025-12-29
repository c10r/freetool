/**
 * Authorization Provider
 *
 * Provides centralized permission management for the application.
 * Fetches and caches user permissions using TanStack Query.
 */

import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { getCurrentUser, getSpacePermissions } from "@/api/api";
import type { Permission, SpacePermissions } from "@/types/permissions";
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
   * Trigger fetching of space permissions
   * Uses TanStack Query's prefetch to populate the cache
   */
  const fetchSpacePermissions = (spaceId: string) => {
    queryClient.prefetchQuery({
      queryKey: ["spacePermissions", spaceId],
      queryFn: async () => {
        const result = await getSpacePermissions(spaceId);
        if (result.error) {
          throw new Error(result.error.message);
        }
        if (!result.data) {
          throw new Error("No space permissions data returned");
        }
        return result.data;
      },
      staleTime: PERMISSION_CACHE_TTL,
    });
  };

  /**
   * Get cached space permissions
   */
  const getSpacePermissionsData = (
    spaceId: string
  ): SpacePermissions | null => {
    const data = queryClient.getQueryData<{
      permissions: SpacePermissions;
    }>(["spacePermissions", spaceId]);
    return data?.permissions || null;
  };

  /**
   * Check if user has a specific permission on a space
   */
  const hasPermission = (spaceId: string, permission: Permission): boolean => {
    const permissions = getSpacePermissionsData(spaceId);
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
   * Check if current user is a moderator of a specific space
   */
  const isSpaceModerator = (spaceId: string): boolean => {
    if (!currentUser) {
      return false;
    }
    return (
      currentUser.spaces.some(
        (space) => space.id === spaceId && space.role === "moderator"
      ) ?? false
    );
  };

  /**
   * Refetch space permissions (invalidate cache and refetch)
   */
  const refetchSpacePermissions = (spaceId: string) => {
    queryClient.invalidateQueries({
      queryKey: ["spacePermissions", spaceId],
    });
  };

  const value: AuthorizationContextValue = {
    currentUser,
    isLoadingUser,
    userError: userError as Error | null,
    fetchSpacePermissions,
    getSpacePermissions: getSpacePermissionsData,
    hasPermission,
    isOrgAdmin,
    isSpaceModerator,
    refetchUser,
    refetchSpacePermissions,
  };

  return (
    <AuthorizationContext.Provider value={value}>
      {children}
    </AuthorizationContext.Provider>
  );
}

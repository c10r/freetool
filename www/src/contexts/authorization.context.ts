/**
 * Authorization Context
 *
 * Core context definition and types for authorization management.
 * This file contains only the context and types - the provider is in AuthorizationContext.tsx
 */

import { createContext } from "react";
import type {
  CurrentUser,
  Permission,
  SpacePermissions,
} from "@/types/permissions";

/**
 * Shape of the authorization context value
 */
export interface AuthorizationContextValue {
  currentUser: CurrentUser | null;
  isLoadingUser: boolean;
  userError: Error | null;
  fetchSpacePermissions: (spaceId: string) => void;
  getSpacePermissions: (spaceId: string) => SpacePermissions | null;
  hasPermission: (spaceId: string, permission: Permission) => boolean;
  isOrgAdmin: () => boolean;
  isSpaceModerator: (spaceId: string) => boolean;
  refetchUser: () => void;
  refetchSpacePermissions: (spaceId: string) => void;
}

/**
 * Authorization context - provides centralized permission management
 */
export const AuthorizationContext = createContext<
  AuthorizationContextValue | undefined
>(undefined);

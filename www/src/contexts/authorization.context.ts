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
  WorkspacePermissions,
} from "@/types/permissions";

/**
 * Shape of the authorization context value
 */
export interface AuthorizationContextValue {
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

/**
 * Authorization context - provides centralized permission management
 */
export const AuthorizationContext = createContext<
  AuthorizationContextValue | undefined
>(undefined);

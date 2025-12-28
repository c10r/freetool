/**
 * Permissions Module - Barrel Export
 *
 * Convenient re-exports of all permission-related functionality.
 * Import everything you need from a single module.
 *
 * @example
 * ```tsx
 * import {
 *   useHasPermission,
 *   PermissionButton,
 *   PermissionGate,
 *   type Permission,
 * } from '@/lib/permissions';
 * ```
 */

// Types
export type {
  Permission,
  WorkspacePermissions,
  CurrentUser,
  TeamMembership,
  CurrentUserResponse,
  WorkspacePermissionsResponse,
} from "@/types/permissions";

// Hooks
export {
  useWorkspacePermissions,
  useHasPermission,
  useCurrentUser,
  useIsOrgAdmin,
  useIsTeamAdmin,
  useHasAnyPermission,
  useUserPermissions,
} from "@/hooks/usePermissions";

// Context
export {
  AuthorizationProvider,
  useAuthorization,
} from "@/contexts/AuthorizationContext";

// Components
export { PermissionGate } from "@/components/PermissionGate";
export type { PermissionGateProps } from "@/components/PermissionGate";

export { PermissionButton } from "@/components/PermissionButton";
export type { PermissionButtonProps } from "@/components/PermissionButton";

export {
  PermissionSkeleton,
  InlinePermissionSkeleton,
} from "@/components/PermissionSkeleton";
export type { PermissionSkeletonProps } from "@/components/PermissionSkeleton";

// Utilities
export {
  getPermissionMessage,
  getPermissionLabel,
  permissionMessages,
  roleMessages,
} from "@/lib/permissionMessages";

// API
export { getCurrentUser, getWorkspacePermissions } from "@/api/api";

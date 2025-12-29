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

// API
export { getCurrentUser, getSpacePermissions } from "@/api/api";
export type { PermissionButtonProps } from "@/components/PermissionButton";
export { PermissionButton } from "@/components/PermissionButton";
export type { PermissionGateProps } from "@/components/PermissionGate";

// Components
export { PermissionGate } from "@/components/PermissionGate";
export type { PermissionSkeletonProps } from "@/components/PermissionSkeleton";
export {
  InlinePermissionSkeleton,
  PermissionSkeleton,
} from "@/components/PermissionSkeleton";
// Context
export {
  type AuthorizationContextValue,
  AuthorizationProvider,
} from "@/contexts/AuthorizationContext";
export { useAuthorization } from "@/hooks/useAuthorization";
// Hooks
export {
  useCurrentUser,
  useHasAnyPermission,
  useHasPermission,
  useIsOrgAdmin,
  useIsTeamAdmin,
  useSpacePermissions,
  useUserPermissions,
} from "@/hooks/usePermissions";

// Utilities
export {
  getPermissionLabel,
  getPermissionMessage,
  permissionMessages,
  roleMessages,
} from "@/lib/permissionMessages";
// Types
export type {
  CurrentUser,
  CurrentUserResponse,
  Permission,
  SpaceMembership,
  SpacePermissions,
  SpacePermissionsResponse,
} from "@/types/permissions";

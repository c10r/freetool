/**
 * User-friendly permission messages for the authorization system
 *
 * These messages are displayed when users attempt actions they don't have
 * permission to perform. They provide clear feedback and guidance on how
 * to request access.
 */

import type { Permission } from "@/types/permissions";

/**
 * Mapping of permissions to user-friendly error messages
 */
export const permissionMessages: Record<Permission, string> = {
  create_resource:
    "You don't have permission to create API resources. Contact your space moderator to request access.",
  edit_resource:
    "You don't have permission to modify this resource. Contact your space moderator to request access.",
  delete_resource:
    "You don't have permission to delete this resource. Contact your space moderator to request access.",
  create_app:
    "You don't have permission to create applications. Contact your space moderator to request access.",
  edit_app:
    "You don't have permission to modify this application. Contact your space moderator to request access.",
  delete_app:
    "You don't have permission to delete this application. Contact your space moderator to request access.",
  run_app:
    "You don't have permission to execute this application. Contact your space moderator to request access.",
  create_folder:
    "You don't have permission to create folders. Contact your space moderator to request access.",
  edit_folder:
    "You don't have permission to rename or move folders. Contact your space moderator to request access.",
  delete_folder:
    "You don't have permission to delete folders. Contact your space moderator to request access.",
};

/**
 * Additional messages for role-based permissions
 */
export const roleMessages = {
  org_admin: "Only organization administrators can perform this action.",
  moderator: "Only space moderators can perform this action.",
  member:
    "This action is restricted to space members with specific permissions.",
};

/**
 * Get a user-friendly permission message
 *
 * @param permission - The permission being checked
 * @param customMessage - Optional custom message to use instead of the default
 * @returns A user-friendly message explaining the permission requirement
 */
export function getPermissionMessage(
  permission: Permission,
  customMessage?: string
): string {
  return (
    customMessage ||
    permissionMessages[permission] ||
    "You don't have permission to perform this action. Contact your space moderator for access."
  );
}

/**
 * Get a short permission label for UI display
 *
 * @param permission - The permission to get a label for
 * @returns A short, human-readable label
 */
export function getPermissionLabel(permission: Permission): string {
  const labels: Record<Permission, string> = {
    create_resource: "Create Resources",
    edit_resource: "Edit Resources",
    delete_resource: "Delete Resources",
    create_app: "Create Apps",
    edit_app: "Edit Apps",
    delete_app: "Delete Apps",
    run_app: "Run Apps",
    create_folder: "Create Folders",
    edit_folder: "Edit Folders",
    delete_folder: "Delete Folders",
  };

  return labels[permission] || permission;
}

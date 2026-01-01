/**
 * PermissionTooltipContent Component
 *
 * Renders tooltip content for permission-denied states with a link
 * to the permissions page where users can see and contact moderators.
 */

import { Link } from "react-router-dom";
import type { Permission } from "@/types/permissions";

/**
 * Maps permission to a human-readable action description
 */
function getActionDescription(permission: Permission): string {
  const descriptions: Record<Permission, string> = {
    create_resource: "create API resources",
    edit_resource: "modify this resource",
    delete_resource: "delete this resource",
    create_app: "create applications",
    edit_app: "modify this application",
    delete_app: "delete this application",
    run_app: "run this application",
    create_folder: "create folders",
    edit_folder: "rename or move folders",
    delete_folder: "delete folders",
  };
  return descriptions[permission];
}

export interface PermissionTooltipContentProps {
  /**
   * The permission that was denied
   */
  permission: Permission;

  /**
   * The space ID for the permissions page link
   */
  spaceId: string;

  /**
   * Custom message to display instead of the default
   * (for backward compatibility)
   */
  customMessage?: string;
}

/**
 * Tooltip content component that shows a permission-denied message
 * with a link to the permissions page.
 *
 * @example
 * ```tsx
 * <TooltipContent>
 *   <PermissionTooltipContent
 *     permission="delete_app"
 *     spaceId="space-123"
 *   />
 * </TooltipContent>
 * ```
 */
export function PermissionTooltipContent({
  permission,
  spaceId,
  customMessage,
}: PermissionTooltipContentProps) {
  // If custom message provided, use it as-is (backward compatibility)
  if (customMessage) {
    return <p>{customMessage}</p>;
  }

  const permissionsUrl = `/spaces/${spaceId}/permissions`;
  const action = getActionDescription(permission);

  return (
    <p>
      You don't have permission to {action}.{" "}
      <Link
        to={permissionsUrl}
        className="underline text-blue-400 hover:text-blue-300"
      >
        Ask a moderator
      </Link>{" "}
      for permissions.
    </p>
  );
}

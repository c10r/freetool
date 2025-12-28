/**
 * PermissionButton Component
 *
 * A button that automatically disables itself when the user lacks permission.
 * Shows a tooltip explaining why the button is disabled.
 */

import { Button, type ButtonProps } from "@/components/ui/button";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useHasPermission } from "@/hooks/usePermissions";
import { getPermissionMessage } from "@/lib/permissionMessages";
import { cn } from "@/lib/utils";
import type { Permission } from "@/types/permissions";

/**
 * Props for the PermissionButton component
 */
export interface PermissionButtonProps extends ButtonProps {
  /**
   * The workspace ID to check permissions for
   */
  workspaceId: string;

  /**
   * The permission required to enable the button
   */
  permission: Permission;

  /**
   * Custom tooltip message when permission is denied
   * (uses default permission message if not provided)
   */
  tooltipMessage?: string;

  /**
   * If true, hides the button entirely instead of disabling it
   * Default: false (shows disabled button)
   */
  hideWhenDisabled?: boolean;
}

/**
 * Button component that automatically disables based on permissions
 *
 * @example
 * ```tsx
 * <PermissionButton
 *   workspaceId={workspaceId}
 *   permission="delete_app"
 *   variant="destructive"
 *   onClick={handleDelete}
 * >
 *   Delete App
 * </PermissionButton>
 *
 * // With custom tooltip
 * <PermissionButton
 *   workspaceId={workspaceId}
 *   permission="create_folder"
 *   tooltipMessage="Premium feature - upgrade to create folders"
 *   onClick={handleCreate}
 * >
 *   Create Folder
 * </PermissionButton>
 *
 * // Hide instead of disable
 * <PermissionButton
 *   workspaceId={workspaceId}
 *   permission="edit_app"
 *   hideWhenDisabled
 *   onClick={handleEdit}
 * >
 *   Edit
 * </PermissionButton>
 * ```
 */
export function PermissionButton({
  workspaceId,
  permission,
  tooltipMessage,
  hideWhenDisabled = false,
  children,
  className,
  disabled,
  ...props
}: PermissionButtonProps) {
  const hasPermission = useHasPermission(workspaceId, permission);
  const isDisabled = !hasPermission || disabled;

  // Hide button if requested and no permission
  if (hideWhenDisabled && !hasPermission) {
    return null;
  }

  const button = (
    <Button
      {...props}
      disabled={isDisabled}
      className={cn(className, !hasPermission && "cursor-not-allowed")}
    >
      {children}
    </Button>
  );

  // If user lacks permission, wrap in tooltip
  if (!hasPermission) {
    const message = getPermissionMessage(permission, tooltipMessage);

    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>{button}</TooltipTrigger>
          <TooltipContent>
            <p>{message}</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );
  }

  // User has permission - render button normally (may still be disabled for other reasons)
  return button;
}

/**
 * PermissionGate Component
 *
 * Conditionally renders children based on user permissions.
 * Can either hide content entirely or show it in a disabled state with a tooltip.
 */

import React, { ReactNode } from "react";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useHasPermission } from "@/hooks/usePermissions";
import type { Permission } from "@/types/permissions";
import { getPermissionMessage } from "@/lib/permissionMessages";

/**
 * Props for the PermissionGate component
 */
export interface PermissionGateProps {
  /**
   * The workspace ID to check permissions for
   */
  workspaceId: string;

  /**
   * The permission required to view the content
   */
  permission: Permission;

  /**
   * Content to render when user has permission
   */
  children: ReactNode;

  /**
   * Content to render when user lacks permission (default: null)
   */
  fallback?: ReactNode;

  /**
   * If true, shows children in disabled state with tooltip instead of hiding
   */
  showTooltip?: boolean;

  /**
   * Custom tooltip message (uses default permission message if not provided)
   */
  tooltipMessage?: string;
}

/**
 * Conditionally renders children based on user permissions
 *
 * @example
 * ```tsx
 * // Hide content when no permission
 * <PermissionGate workspaceId={wsId} permission="delete_app">
 *   <DeleteButton />
 * </PermissionGate>
 *
 * // Show disabled with tooltip
 * <PermissionGate
 *   workspaceId={wsId}
 *   permission="delete_app"
 *   showTooltip
 *   tooltipMessage="You cannot delete this app"
 * >
 *   <DeleteButton />
 * </PermissionGate>
 *
 * // Show fallback content
 * <PermissionGate
 *   workspaceId={wsId}
 *   permission="create_app"
 *   fallback={<div>Contact admin to create apps</div>}
 * >
 *   <CreateAppButton />
 * </PermissionGate>
 * ```
 */
export function PermissionGate({
  workspaceId,
  permission,
  children,
  fallback = null,
  showTooltip = false,
  tooltipMessage,
}: PermissionGateProps) {
  const hasPermission = useHasPermission(workspaceId, permission);

  // User has permission - render children normally
  if (hasPermission) {
    return <>{children}</>;
  }

  // User lacks permission and tooltip is requested
  if (showTooltip && children) {
    const message = getPermissionMessage(permission, tooltipMessage);

    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <div className="inline-block opacity-50 cursor-not-allowed">
              {children}
            </div>
          </TooltipTrigger>
          <TooltipContent>
            <p>{message}</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );
  }

  // User lacks permission - show fallback or nothing
  return <>{fallback}</>;
}

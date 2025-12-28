/**
 * PermissionSkeleton Component
 *
 * Loading skeleton to display while permissions are being fetched.
 * Provides visual feedback during authorization checks.
 */

import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

/**
 * Props for PermissionSkeleton component
 */
export interface PermissionSkeletonProps {
  /**
   * Optional className for custom styling
   */
  className?: string;

  /**
   * Number of skeleton items to show (default: 3)
   */
  count?: number;

  /**
   * Type of skeleton to show
   * - 'button': Shows button-shaped skeletons
   * - 'card': Shows card-shaped skeletons
   * - 'list': Shows list item skeletons
   * - 'page': Shows full page skeleton
   */
  type?: "button" | "card" | "list" | "page";
}

/**
 * Loading skeleton for permission-gated content
 *
 * @example
 * ```tsx
 * function WorkspaceView({ workspaceId }) {
 *   const { permissions, isLoading } = useWorkspacePermissions(workspaceId);
 *
 *   if (isLoading) {
 *     return <PermissionSkeleton type="page" />;
 *   }
 *
 *   return <WorkspaceContent />;
 * }
 *
 * // Button skeleton
 * <PermissionSkeleton type="button" count={2} />
 *
 * // Card skeleton
 * <PermissionSkeleton type="card" count={4} />
 * ```
 */
export function PermissionSkeleton({
  className,
  count = 3,
  type = "card",
}: PermissionSkeletonProps) {
  if (type === "button") {
    return (
      <div className={cn("flex gap-2", className)}>
        {Array.from({ length: count }, (_, i) => `skeleton-button-${i}`).map(
          (key) => (
            <Skeleton key={key} className="h-10 w-24" />
          )
        )}
      </div>
    );
  }

  if (type === "list") {
    return (
      <div className={cn("space-y-2", className)}>
        {Array.from({ length: count }, (_, i) => `skeleton-list-${i}`).map(
          (key) => (
            <Skeleton key={key} className="h-12 w-full" />
          )
        )}
      </div>
    );
  }

  if (type === "page") {
    return (
      <div className={cn("space-y-4", className)}>
        <Skeleton className="h-12 w-64" />
        <div className="flex gap-2">
          <Skeleton className="h-10 w-32" />
          <Skeleton className="h-10 w-32" />
        </div>
        <Skeleton className="h-64 w-full" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      </div>
    );
  }

  // Default: card
  return (
    <div
      className={cn(
        "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4",
        className
      )}
    >
      {Array.from({ length: count }, (_, i) => `skeleton-card-${i}`).map(
        (key) => (
          <Skeleton key={key} className="h-32 w-full" />
        )
      )}
    </div>
  );
}

/**
 * Simple skeleton for inline loading states
 */
export function InlinePermissionSkeleton({
  className,
}: {
  className?: string;
}) {
  return <Skeleton className={cn("h-4 w-20", className)} />;
}

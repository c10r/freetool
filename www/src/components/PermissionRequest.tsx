/**
 * PermissionRequest Component
 *
 * A component that helps users request permissions they don't have.
 * Shows a helpful message and provides a way to contact team admins.
 */

import { AlertCircle, Mail } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useToast } from "@/hooks/use-toast";
import { getPermissionLabel } from "@/lib/permissionMessages";
import type { Permission } from "@/types/permissions";

/**
 * Props for the PermissionRequest component
 */
export interface PermissionRequestProps {
  /**
   * The space ID the permission is needed for
   */
  spaceId: string;

  /**
   * The specific permission being requested
   */
  permission: Permission;

  /**
   * Optional callback when request is sent
   */
  onRequestSent?: () => void;

  /**
   * Optional custom message to display
   */
  customMessage?: string;
}

/**
 * Component that helps users request permissions
 *
 * @example
 * ```tsx
 * <PermissionRequest
 *   spaceId={spaceId}
 *   permission="create_app"
 *   onRequestSent={() => console.log('Request sent')}
 * />
 * ```
 */
export function PermissionRequest({
  spaceId: _spaceId,
  permission,
  onRequestSent,
  customMessage,
}: PermissionRequestProps) {
  const [isRequesting, setIsRequesting] = useState(false);
  const { toast } = useToast();

  const handleRequestPermission = async () => {
    setIsRequesting(true);

    try {
      // TODO: Implement actual permission request API when available
      // For now, just show a toast with instructions

      // Simulate API delay
      await new Promise((resolve) => setTimeout(resolve, 500));

      toast({
        title: "Permission Request Guidance",
        description: `To request ${getPermissionLabel(permission)}, please contact your team administrator or organization administrator. They can grant you access to this feature.`,
        duration: 6000,
      });

      onRequestSent?.();
    } catch (_error) {
      toast({
        title: "Error",
        description: "Failed to send permission request. Please try again.",
        variant: "destructive",
        duration: 5000,
      });
    } finally {
      setIsRequesting(false);
    }
  };

  return (
    <Card className="border-blue-200 bg-blue-50">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-blue-900">
          <AlertCircle className="w-5 h-5" />
          Permission Required
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div>
          <p className="text-sm text-blue-900 mb-2">
            {customMessage || (
              <>
                You need <strong>{getPermissionLabel(permission)}</strong>{" "}
                permission to perform this action.
              </>
            )}
          </p>
          <p className="text-sm text-blue-800">
            Contact your team administrator to request access. They can grant
            you the necessary permissions for this workspace.
          </p>
        </div>

        <div className="flex gap-2">
          <Button
            onClick={handleRequestPermission}
            disabled={isRequesting}
            size="sm"
            variant="default"
          >
            <Mail className="w-4 h-4 mr-2" />
            {isRequesting ? "Requesting..." : "Request Permission"}
          </Button>
        </div>

        <div className="text-xs text-blue-700 bg-blue-100 p-3 rounded">
          <strong>Tip:</strong> Space moderators and organization administrators
          can manage permissions from the Users & Spaces section.
        </div>
      </CardContent>
    </Card>
  );
}

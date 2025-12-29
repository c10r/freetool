import {
  Building2,
  CheckCircle,
  Crown,
  Shield,
  User,
  XCircle,
} from "lucide-react";
import { useParams } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useCurrentUser, useSpacePermissions } from "@/hooks/usePermissions";
import { useSpaces } from "@/hooks/useSpaces";
import { getPermissionLabel } from "@/lib/permissionMessages";
import { cn } from "@/lib/utils";
import type { Permission } from "@/types/permissions";

export default function MyPermissions() {
  const { currentUser, isLoading: userLoading } = useCurrentUser();
  const { spaceId } = useParams<{ spaceId?: string }>();
  const { spaces, isLoading: spacesLoading } = useSpaces();

  // Use the first space if no spaceId in URL
  const activeSpaceId = spaceId || (spaces.length > 0 ? spaces[0].id : "");
  const { permissions, isLoading: permissionsLoading } =
    useSpacePermissions(activeSpaceId);

  const isLoading = userLoading || permissionsLoading || spacesLoading;

  if (isLoading) {
    return (
      <div className="p-6 space-y-6 overflow-y-auto flex-1">
        <h1 className="text-2xl font-bold mb-4">My Permissions</h1>
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-4 w-full" />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-4 w-full" />
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 overflow-y-auto flex-1">
      <header>
        <h1 className="text-2xl font-bold mb-2">My Permissions</h1>
        <p className="text-muted-foreground">
          View your current roles and permissions across the organization
        </p>
      </header>

      {/* User Profile */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <User className="w-5 h-5" />
            Profile
          </CardTitle>
        </CardHeader>
        <CardContent>
          {currentUser ? (
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                {currentUser.profilePicUrl ? (
                  <img
                    src={currentUser.profilePicUrl}
                    alt={`${currentUser.name}'s profile`}
                    className="w-12 h-12 rounded-full object-cover"
                  />
                ) : (
                  <div className="w-12 h-12 rounded-full bg-muted flex items-center justify-center">
                    <User className="w-6 h-6 text-muted-foreground" />
                  </div>
                )}
                <div>
                  <p className="font-medium">{currentUser.name}</p>
                  <p className="text-sm text-muted-foreground">
                    {currentUser.email}
                  </p>
                </div>
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              No user information available
            </p>
          )}
        </CardContent>
      </Card>

      {/* Organization Role */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5" />
            Organization Role
          </CardTitle>
        </CardHeader>
        <CardContent>
          {currentUser?.isOrgAdmin ? (
            <Badge variant="default" className="text-sm">
              <Shield className="w-3 h-3 mr-1" />
              Organization Administrator
            </Badge>
          ) : (
            <Badge variant="secondary" className="text-sm">
              <User className="w-3 h-3 mr-1" />
              Member
            </Badge>
          )}
          <p className="text-sm text-muted-foreground mt-2">
            {currentUser?.isOrgAdmin
              ? "You have full administrative access to all spaces."
              : "You have access based on your space membership and granted permissions."}
          </p>
        </CardContent>
      </Card>

      {/* Spaces */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Building2 className="w-5 h-5" />
            Spaces
          </CardTitle>
        </CardHeader>
        <CardContent>
          {currentUser && currentUser.spaces.length > 0 ? (
            <div className="space-y-2">
              {currentUser.spaces.map((space) => (
                <div
                  key={space.id}
                  className="flex items-center justify-between p-3 border rounded-lg"
                >
                  <span className="font-medium">{space.name}</span>
                  {space.role === "moderator" ? (
                    <Badge
                      variant="outline"
                      className="border-amber-500 text-amber-600"
                    >
                      <Crown className="w-3 h-3 mr-1" />
                      Moderator
                    </Badge>
                  ) : (
                    <Badge variant="secondary">
                      <User className="w-3 h-3 mr-1" />
                      Member
                    </Badge>
                  )}
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              You are not a member of any spaces yet.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Space Permissions */}
      <Card>
        <CardHeader>
          <CardTitle>Space Permissions</CardTitle>
          <p className="text-sm text-muted-foreground mt-1">
            Space: {activeSpaceId || "No space selected"}
          </p>
        </CardHeader>
        <CardContent>
          {permissions ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {(Object.entries(permissions) as [Permission, boolean][]).map(
                ([permission, hasPermission]) => (
                  <div
                    key={permission}
                    className={cn(
                      "flex items-center gap-3 p-3 border rounded-lg",
                      hasPermission
                        ? "bg-green-50 border-green-200"
                        : "bg-gray-50 border-gray-200"
                    )}
                  >
                    {hasPermission ? (
                      <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0" />
                    ) : (
                      <XCircle className="w-5 h-5 text-gray-400 flex-shrink-0" />
                    )}
                    <span
                      className={cn(
                        "text-sm font-medium",
                        hasPermission
                          ? "text-green-900"
                          : "text-muted-foreground"
                      )}
                    >
                      {getPermissionLabel(permission)}
                    </span>
                  </div>
                )
              )}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              No permissions information available for this space.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Help Text */}
      <Card className="border-blue-200 bg-blue-50">
        <CardContent className="py-4">
          <p className="text-sm text-blue-900">
            <strong>Need additional permissions?</strong> Contact your space
            moderator or organization administrator to request access to
            specific features.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

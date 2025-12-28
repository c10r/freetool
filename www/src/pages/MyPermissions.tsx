import { CheckCircle, Shield, User, Users, XCircle } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useCurrentUser,
  useWorkspacePermissions,
} from "@/hooks/usePermissions";
import { getPermissionLabel } from "@/lib/permissionMessages";
import { cn } from "@/lib/utils";
import type { Permission } from "@/types/permissions";

export default function MyPermissions() {
  const { currentUser, isLoading: userLoading } = useCurrentUser();

  // Hardcode workspace ID for now (TODO: get from context or fetch all workspaces)
  const workspaceId = "workspace-main";
  const { permissions, isLoading: permissionsLoading } =
    useWorkspacePermissions(workspaceId);

  const isLoading = userLoading || permissionsLoading;

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
              Organization Administrator
            </Badge>
          ) : (
            <Badge variant="secondary" className="text-sm">
              Team Member
            </Badge>
          )}
          <p className="text-sm text-muted-foreground mt-2">
            {currentUser?.isOrgAdmin
              ? "You have full administrative access to all workspaces and teams."
              : "You have access based on your team membership and granted permissions."}
          </p>
        </CardContent>
      </Card>

      {/* Teams */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="w-5 h-5" />
            Teams
          </CardTitle>
        </CardHeader>
        <CardContent>
          {currentUser && currentUser.teams.length > 0 ? (
            <div className="space-y-2">
              {currentUser.teams.map((team) => (
                <div
                  key={team.id}
                  className="flex items-center justify-between p-3 border rounded-lg"
                >
                  <span className="font-medium">{team.name}</span>
                  <Badge
                    variant={team.role === "admin" ? "default" : "secondary"}
                  >
                    {team.role === "admin" ? "Team Admin" : "Member"}
                  </Badge>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              You are not a member of any teams yet.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Workspace Permissions */}
      <Card>
        <CardHeader>
          <CardTitle>Workspace Permissions</CardTitle>
          <p className="text-sm text-muted-foreground mt-1">
            Workspace: {workspaceId}
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
              No permissions information available for this workspace.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Help Text */}
      <Card className="border-blue-200 bg-blue-50">
        <CardContent className="py-4">
          <p className="text-sm text-blue-900">
            <strong>Need additional permissions?</strong> Contact your team
            administrator or organization administrator to request access to
            specific features.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

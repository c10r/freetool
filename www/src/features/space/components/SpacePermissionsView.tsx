import {
  ChevronLeft,
  Crown,
  GlobeLock,
  Save,
  User as UserIcon,
} from "lucide-react";
import { useState } from "react";
import { useParams } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useIsOrgAdmin, useIsSpaceModerator } from "@/hooks/usePermissions";
import { useSpaceMembersPermissions } from "@/hooks/useSpaceMembersPermissions";
import type { Permission, SpacePermissions } from "@/types/permissions";

interface SpacePermissionsViewProps {
  onBackClick?: () => void;
}

/**
 * Permission groups for organizing the table columns
 */
const PERMISSION_GROUPS = [
  {
    label: "Resources",
    permissions: [
      { key: "create_resource" as Permission, label: "Create" },
      { key: "edit_resource" as Permission, label: "Edit" },
      { key: "delete_resource" as Permission, label: "Delete" },
    ],
  },
  {
    label: "Apps",
    permissions: [
      { key: "create_app" as Permission, label: "Create" },
      { key: "edit_app" as Permission, label: "Edit" },
      { key: "delete_app" as Permission, label: "Delete" },
      { key: "run_app" as Permission, label: "Run" },
    ],
  },
  {
    label: "Folders",
    permissions: [
      { key: "create_folder" as Permission, label: "Create" },
      { key: "edit_folder" as Permission, label: "Edit" },
      { key: "delete_folder" as Permission, label: "Delete" },
    ],
  },
];

/**
 * SpacePermissionsView Component
 *
 * Displays and manages permissions for all members of a space.
 * Moderators and org admins can edit permissions, while regular members
 * can only view them.
 */
export default function SpacePermissionsView({
  onBackClick,
}: SpacePermissionsViewProps) {
  const { spaceId } = useParams<{ spaceId: string }>();

  // State for tracking pending permission changes
  const [pendingChanges, setPendingChanges] = useState<
    Record<string, SpacePermissions>
  >({});
  const [isSaving, setIsSaving] = useState(false);

  // Fetch members and their permissions
  const { members, spaceName, isLoading, error, updatePermissions } =
    useSpaceMembersPermissions(spaceId || "");

  // Permission checks for editing
  const isOrgAdmin = useIsOrgAdmin();
  const isSpaceModerator = useIsSpaceModerator(spaceId || "");
  const canEdit = isOrgAdmin || isSpaceModerator;

  /**
   * Get the effective permissions for a user (original merged with pending changes)
   */
  const getEffectivePermissions = (
    userId: string,
    originalPermissions: SpacePermissions
  ): SpacePermissions => {
    return pendingChanges[userId] || originalPermissions;
  };

  /**
   * Handle toggling a permission for a user
   */
  const handlePermissionChange = (
    userId: string,
    permissionKey: Permission,
    currentPermissions: SpacePermissions
  ) => {
    const effectivePermissions = getEffectivePermissions(
      userId,
      currentPermissions
    );
    const newValue = !effectivePermissions[permissionKey];

    setPendingChanges((prev) => ({
      ...prev,
      [userId]: {
        ...effectivePermissions,
        [permissionKey]: newValue,
      },
    }));
  };

  /**
   * Save all pending changes
   */
  const handleSaveChanges = async () => {
    if (Object.keys(pendingChanges).length === 0) {
      return;
    }

    setIsSaving(true);
    try {
      // Update permissions for each user with pending changes
      for (const [userId, permissions] of Object.entries(pendingChanges)) {
        await updatePermissions(userId, permissions);
      }
      // Clear pending changes after successful save
      setPendingChanges({});
    } catch (_err) {
      // Error handling is done by the mutation - toast notifications are shown automatically
    } finally {
      setIsSaving(false);
    }
  };

  const hasPendingChanges = Object.keys(pendingChanges).length > 0;

  if (isLoading) {
    return (
      <section className="p-6 space-y-4 overflow-y-auto flex-1">
        <header className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            {onBackClick && (
              <Button variant="ghost" size="sm" onClick={onBackClick}>
                <ChevronLeft className="h-4 w-4 mr-1" />
                Back
              </Button>
            )}
            <Skeleton className="h-8 w-48" />
          </div>
        </header>
        <Separator />
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-32" />
          </CardHeader>
          <CardContent className="space-y-4">
            <Skeleton className="h-64 w-full" />
          </CardContent>
        </Card>
      </section>
    );
  }

  if (error) {
    return (
      <section className="p-6 space-y-4 overflow-y-auto flex-1">
        <header className="flex items-center gap-2">
          {onBackClick && (
            <Button variant="ghost" size="sm" onClick={onBackClick}>
              <ChevronLeft className="h-4 w-4 mr-1" />
              Back
            </Button>
          )}
          <h2 className="text-2xl font-semibold">Permissions</h2>
        </header>
        <Separator />
        <Card>
          <CardContent className="py-10 text-center text-red-500">
            {error.message || "Failed to load permissions"}
          </CardContent>
        </Card>
      </section>
    );
  }

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {onBackClick && (
            <Button variant="ghost" size="sm" onClick={onBackClick}>
              <ChevronLeft className="h-4 w-4 mr-1" />
              Back
            </Button>
          )}
          <h2 className="text-2xl font-semibold">
            {spaceName ? `${spaceName} - Permissions` : "Permissions"}
          </h2>
        </div>
        {canEdit && hasPendingChanges && (
          <Button onClick={handleSaveChanges} disabled={isSaving}>
            <Save className="h-4 w-4 mr-2" />
            {isSaving ? "Saving..." : "Save Changes"}
          </Button>
        )}
      </header>
      <Separator />

      {members.length === 0 ? (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No members found in this space.
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Member Permissions</CardTitle>
            {!canEdit && (
              <p className="text-sm text-muted-foreground">
                You can view permissions but only moderators and administrators
                can edit them.
              </p>
            )}
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  {/* Group headers row */}
                  <TableRow className="border-b-0">
                    <TableHead
                      className="sticky left-0 bg-background z-10 w-48"
                      rowSpan={2}
                    >
                      Member
                    </TableHead>
                    {PERMISSION_GROUPS.map((group) => (
                      <TableHead
                        key={group.label}
                        colSpan={group.permissions.length}
                        className="text-center border-l"
                      >
                        {group.label}
                      </TableHead>
                    ))}
                  </TableRow>
                  {/* Permission sub-headers row */}
                  <TableRow>
                    {PERMISSION_GROUPS.map((group) =>
                      group.permissions.map((perm, idx) => (
                        <TableHead
                          key={perm.key}
                          className={`text-center text-xs ${
                            idx === 0 ? "border-l" : ""
                          }`}
                        >
                          {perm.label}
                        </TableHead>
                      ))
                    )}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {members.map((member) => {
                    const effectivePermissions = getEffectivePermissions(
                      member.userId,
                      member.permissions
                    );
                    const hasChanges = !!pendingChanges[member.userId];

                    return (
                      <TableRow
                        key={member.userId}
                        className={hasChanges ? "bg-yellow-50" : ""}
                      >
                        {/* Member name cell - sticky */}
                        <TableCell className="sticky left-0 bg-background z-10 font-medium">
                          <div className="flex items-center gap-2">
                            {member.profilePicUrl ? (
                              <img
                                src={member.profilePicUrl}
                                alt={member.userName}
                                className="w-8 h-8 rounded-full object-cover"
                              />
                            ) : (
                              <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center">
                                <UserIcon
                                  size={16}
                                  className="text-muted-foreground"
                                />
                              </div>
                            )}
                            <div className="flex flex-col">
                              <span className="flex items-center gap-1">
                                {member.userName}
                                {member.isModerator && (
                                  <TooltipProvider>
                                    <Tooltip>
                                      <TooltipTrigger>
                                        <Crown className="h-4 w-4 text-amber-500" />
                                      </TooltipTrigger>
                                      <TooltipContent>
                                        <p>Space Moderator</p>
                                      </TooltipContent>
                                    </Tooltip>
                                  </TooltipProvider>
                                )}
                                {member.isOrgAdmin && (
                                  <TooltipProvider>
                                    <Tooltip>
                                      <TooltipTrigger>
                                        <GlobeLock className="h-4 w-4 text-blue-500" />
                                      </TooltipTrigger>
                                      <TooltipContent>
                                        <p>Organization Admin</p>
                                      </TooltipContent>
                                    </Tooltip>
                                  </TooltipProvider>
                                )}
                              </span>
                              <span className="text-xs text-muted-foreground">
                                {member.userEmail}
                              </span>
                            </div>
                            {hasChanges && (
                              <Badge variant="outline" className="ml-2 text-xs">
                                Unsaved
                              </Badge>
                            )}
                          </div>
                        </TableCell>

                        {/* Permission checkboxes */}
                        {PERMISSION_GROUPS.map((group) =>
                          group.permissions.map((perm, idx) => {
                            const isChecked = effectivePermissions[perm.key];
                            const isDisabled = !canEdit || member.isModerator;

                            return (
                              <TableCell
                                key={perm.key}
                                className={`text-center ${
                                  idx === 0 ? "border-l" : ""
                                }`}
                              >
                                <div className="flex justify-center">
                                  <Checkbox
                                    checked={isChecked}
                                    disabled={isDisabled}
                                    onCheckedChange={() =>
                                      handlePermissionChange(
                                        member.userId,
                                        perm.key,
                                        member.permissions
                                      )
                                    }
                                    aria-label={`${perm.label} ${group.label.toLowerCase()} permission for ${member.userName}`}
                                  />
                                </div>
                              </TableCell>
                            );
                          })
                        )}
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            {/* Legend */}
            <div className="mt-4 pt-4 border-t flex gap-4 text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <Crown className="h-4 w-4 text-amber-500" />
                <span>Moderator (all permissions, cannot be changed)</span>
              </div>
              <div className="flex items-center gap-2">
                <GlobeLock className="h-4 w-4 text-blue-500" />
                <span>Organization Admin</span>
              </div>
              {hasPendingChanges && (
                <div className="flex items-center gap-2">
                  <Badge variant="outline" className="text-xs">
                    Unsaved
                  </Badge>
                  <span>Has pending changes</span>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </section>
  );
}

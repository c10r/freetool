import { useQueryClient } from "@tanstack/react-query";
import { Edit, Plus, User as UserIcon, Users } from "lucide-react";
import { useEffect, useState } from "react";
import {
  addGroupMember,
  createGroup,
  getGroupById,
  getGroups,
  getUsers,
  inviteUser,
  removeGroupMember,
  updateGroupName,
} from "@/api/api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useIsOrgAdmin } from "@/hooks/usePermissions";

interface User {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
  invitedAt?: string; // ISO date string, present if user is invited placeholder
}

interface Group {
  id: string;
  name: string;
  memberCount?: number;
}

const isInvitedPlaceholder = (user: User): boolean =>
  !!user.invitedAt && (!user.name || user.name === "");

export default function UsersTeamsView() {
  const queryClient = useQueryClient();
  const [users, setUsers] = useState<User[]>([]);
  const [groups, setGroups] = useState<Group[]>([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [groupsLoading, setGroupsLoading] = useState(true);
  const [usersError, setUsersError] = useState<string | null>(null);
  const [groupsError, setGroupsError] = useState<string | null>(null);

  // Role checks
  const isOrgAdmin = useIsOrgAdmin();

  const [createGroupOpen, setCreateGroupOpen] = useState(false);
  const [groupName, setGroupName] = useState("");
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [isCreatingGroup, setIsCreatingGroup] = useState(false);

  const [editGroupOpen, setEditGroupOpen] = useState(false);
  const [editingGroup, setEditingGroup] = useState<Group | null>(null);
  const [editGroupName, setEditGroupName] = useState("");
  const [editSelectedUserIds, setEditSelectedUserIds] = useState<string[]>([]);
  const [isUpdatingGroup, setIsUpdatingGroup] = useState(false);

  // Invite by email state
  const [inviteEmail, setInviteEmail] = useState("");
  const [isInviting, setIsInviting] = useState(false);
  const [editInviteEmail, setEditInviteEmail] = useState("");
  const [isEditInviting, setIsEditInviting] = useState(false);

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        setUsersLoading(true);
        setUsersError(null);
        const response = await getUsers();
        if (response.data) {
          const userData: User[] = response.data.items?.map((user) => {
            return {
              id: user.id,
              name: user.name,
              email: user.email,
              profilePicUrl: user.profilePicUrl,
              invitedAt: user.invitedAt,
            };
          });
          setUsers(userData);
        }
      } catch (_error) {
        setUsersError("Failed to load users");
      } finally {
        setUsersLoading(false);
      }
    };

    const fetchGroups = async () => {
      try {
        setGroupsLoading(true);
        setGroupsError(null);
        const response = await getGroups();
        if (response.data) {
          const groupData: Group[] = response.data.items?.map((group) => {
            return {
              id: group.id,
              name: group.name,
              memberCount: group.userIds?.length || 0,
            };
          });
          setGroups(groupData);
        }
      } catch (_error) {
        setGroupsError("Failed to load teams");
      } finally {
        setGroupsLoading(false);
      }
    };

    fetchUsers();
    fetchGroups();
  }, []);

  const handleCreateGroup = async () => {
    if (!groupName.trim() || selectedUserIds.length === 0) {
      return;
    }

    try {
      setIsCreatingGroup(true);
      await createGroup({ name: groupName.trim(), userIds: selectedUserIds });

      setCreateGroupOpen(false);
      setGroupName("");
      setSelectedUserIds([]);

      const response = await getGroups();
      if (response.data) {
        const groupData: Group[] = response.data.items?.map((group) => ({
          id: group.id,
          name: group.name,
          memberCount: group.userIds?.length || 0,
        }));
        setGroups(groupData);
      }

      // Invalidate workspace list to refetch (team creation creates workspace)
      queryClient.invalidateQueries({ queryKey: ["workspaces"] });
    } finally {
      setIsCreatingGroup(false);
    }
  };

  const handleUserSelection = (userId: string, checked: boolean) => {
    if (checked) {
      setSelectedUserIds([...selectedUserIds, userId]);
    } else {
      setSelectedUserIds(selectedUserIds.filter((id) => id !== userId));
    }
  };

  const handleEditUserSelection = (userId: string, checked: boolean) => {
    if (checked) {
      setEditSelectedUserIds([...editSelectedUserIds, userId]);
    } else {
      setEditSelectedUserIds(editSelectedUserIds.filter((id) => id !== userId));
    }
  };

  const handleInviteUser = async () => {
    if (!inviteEmail.trim()) {
      return;
    }

    try {
      setIsInviting(true);
      const response = await inviteUser({ email: inviteEmail.trim() });

      if (response.data) {
        const newUser: User = {
          id: response.data.id,
          name: response.data.name,
          email: response.data.email,
          profilePicUrl: response.data.profilePicUrl,
          invitedAt: response.data.invitedAt,
        };

        setUsers((prev) => [...prev, newUser]);
        // Auto-select the invited user
        setSelectedUserIds((prev) => [...prev, newUser.id]);
        setInviteEmail("");
      }
    } catch (_error) {
      // Could show toast notification in the future
    } finally {
      setIsInviting(false);
    }
  };

  const handleEditInviteUser = async () => {
    if (!editInviteEmail.trim()) {
      return;
    }

    try {
      setIsEditInviting(true);
      const response = await inviteUser({ email: editInviteEmail.trim() });

      if (response.data) {
        const newUser: User = {
          id: response.data.id,
          name: response.data.name,
          email: response.data.email,
          profilePicUrl: response.data.profilePicUrl,
          invitedAt: response.data.invitedAt,
        };

        setUsers((prev) => [...prev, newUser]);
        // Auto-select the invited user for the edit dialog
        setEditSelectedUserIds((prev) => [...prev, newUser.id]);
        setEditInviteEmail("");
      }
    } catch (_error) {
      // Could show toast notification in the future
    } finally {
      setIsEditInviting(false);
    }
  };

  const handleEditGroup = async (group: Group) => {
    setEditingGroup(group);
    setEditGroupName(group.name);

    const response = await getGroupById(group.id);
    if (response.data) {
      const currentUserIds = response.data.userIds || [];
      setEditSelectedUserIds(currentUserIds);
    }

    setEditGroupOpen(true);
  };

  const handleUpdateGroup = async () => {
    if (!(editingGroup && editGroupName.trim())) {
      return;
    }

    try {
      setIsUpdatingGroup(true);

      await updateGroupName({
        id: editingGroup.id,
        name: editGroupName.trim(),
      });

      const response = await getGroupById(editingGroup.id);
      if (response.data) {
        const currentUserIds = response.data.userIds || [];

        const usersToAdd = editSelectedUserIds.filter(
          (id) => !currentUserIds.includes(id)
        );
        const usersToRemove = currentUserIds.filter(
          (id) => !editSelectedUserIds.includes(id)
        );

        for (const userId of usersToAdd) {
          await addGroupMember({ groupId: editingGroup.id, userId });
        }

        for (const userId of usersToRemove) {
          await removeGroupMember({ groupId: editingGroup.id, userId });
        }
      }

      setEditGroupOpen(false);
      setEditingGroup(null);
      setEditGroupName("");
      setEditSelectedUserIds([]);

      const groupsResponse = await getGroups();
      if (groupsResponse.data) {
        const groupData: Group[] = groupsResponse.data.items?.map((group) => ({
          id: group.id,
          name: group.name,
          memberCount: group.userIds?.length || 0,
        }));
        setGroups(groupData);
      }
    } finally {
      setIsUpdatingGroup(false);
    }
  };

  return (
    <div className="p-6 space-y-8 overflow-y-auto flex-1">
      <header>
        <h1 className="text-2xl font-semibold mb-2">Users & Teams</h1>
        <p className="text-muted-foreground">
          Manage users and teams in your organization
        </p>
      </header>

      {/* Users Section */}
      <section className="space-y-4">
        <div className="flex items-center gap-2">
          <UserIcon size={20} />
          <h2 className="text-xl font-semibold">Users</h2>
          {!usersLoading && <Badge variant="secondary">{users.length}</Badge>}
        </div>

        {usersLoading ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }, (_, i) => `skeleton-user-${i}`).map(
              (key) => (
                <Card key={key}>
                  <CardHeader>
                    <Skeleton className="h-4 w-3/4" />
                    <Skeleton className="h-3 w-1/2" />
                  </CardHeader>
                  <CardContent>
                    <Skeleton className="h-3 w-full" />
                  </CardContent>
                </Card>
              )
            )}
          </div>
        ) : usersError ? (
          <Card>
            <CardContent className="py-10 text-center text-destructive">
              {usersError}
            </CardContent>
          </Card>
        ) : users.length === 0 ? (
          <Card>
            <CardContent className="py-10 text-center text-muted-foreground">
              No users found
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {users.map((user) => (
              <Card
                key={user.id}
                className={isInvitedPlaceholder(user) ? "opacity-70" : ""}
              >
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <CardTitle className="text-base font-medium">
                      {isInvitedPlaceholder(user) ? user.email : user.name}
                    </CardTitle>
                    {isInvitedPlaceholder(user) && (
                      <Badge variant="secondary" className="text-xs">
                        Pending
                      </Badge>
                    )}
                  </div>
                  {!isInvitedPlaceholder(user) && (
                    <p className="text-sm text-muted-foreground">
                      {user.email}
                    </p>
                  )}
                </CardHeader>
                <CardContent>
                  {user.profilePicUrl ? (
                    <img
                      src={user.profilePicUrl}
                      alt={`${user.name}'s profile`}
                      className="w-10 h-10 rounded-full object-cover"
                    />
                  ) : (
                    <div className="w-10 h-10 rounded-full bg-muted flex items-center justify-center">
                      <UserIcon size={20} className="text-muted-foreground" />
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>

      {/* Teams Section */}
      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Users size={20} />
            <h2 className="text-xl font-semibold">Teams</h2>
            {!groupsLoading && (
              <Badge variant="secondary">{groups.length}</Badge>
            )}
          </div>
          {isOrgAdmin ? (
            <Dialog open={createGroupOpen} onOpenChange={setCreateGroupOpen}>
              <DialogTrigger asChild>
                <Button size="sm">
                  <Plus size={16} className="mr-2" />
                  Create Team
                </Button>
              </DialogTrigger>
              <DialogContent className="sm:max-w-md">
                <DialogHeader>
                  <DialogTitle>Create New Team</DialogTitle>
                  <DialogDescription>
                    Create a team by giving it a name and selecting users to
                    include.
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="group-name">Team Name</Label>
                    <Input
                      id="group-name"
                      placeholder="Enter team name"
                      value={groupName}
                      onChange={(e) => setGroupName(e.target.value)}
                    />
                  </div>
                  <div className="space-y-3">
                    <Label>Select Users</Label>
                    <div className="space-y-2 max-h-48 overflow-y-auto">
                      {users.map((user) => (
                        <div
                          key={user.id}
                          className="flex items-center space-x-3"
                        >
                          <Checkbox
                            id={`user-${user.id}`}
                            checked={selectedUserIds.includes(user.id)}
                            onCheckedChange={(checked) =>
                              handleUserSelection(user.id, checked as boolean)
                            }
                          />
                          <div className="flex items-center space-x-2 flex-1">
                            {user.profilePicUrl ? (
                              <img
                                src={user.profilePicUrl}
                                alt={`${user.name}'s profile`}
                                className="w-6 h-6 rounded-full object-cover"
                              />
                            ) : (
                              <div className="w-6 h-6 rounded-full bg-muted flex items-center justify-center">
                                <UserIcon
                                  size={12}
                                  className="text-muted-foreground"
                                />
                              </div>
                            )}
                            <Label
                              htmlFor={`user-${user.id}`}
                              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
                            >
                              {isInvitedPlaceholder(user) ? (
                                <span className="flex items-center gap-2">
                                  <span className="text-muted-foreground">
                                    {user.email}
                                  </span>
                                  <Badge
                                    variant="secondary"
                                    className="text-xs"
                                  >
                                    Pending
                                  </Badge>
                                </span>
                              ) : (
                                user.name
                              )}
                            </Label>
                          </div>
                        </div>
                      ))}
                    </div>
                    {selectedUserIds.length > 0 && (
                      <p className="text-xs text-muted-foreground">
                        {selectedUserIds.length} user
                        {selectedUserIds.length !== 1 ? "s" : ""} selected
                      </p>
                    )}
                  </div>
                  <div className="space-y-2 pt-4 border-t">
                    <Label>Or invite by email</Label>
                    <div className="flex gap-2">
                      <Input
                        type="email"
                        placeholder="Enter email address"
                        value={inviteEmail}
                        onChange={(e) => setInviteEmail(e.target.value)}
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            e.preventDefault();
                            handleInviteUser();
                          }
                        }}
                      />
                      <Button
                        type="button"
                        variant="outline"
                        onClick={handleInviteUser}
                        disabled={!inviteEmail.trim() || isInviting}
                      >
                        {isInviting ? "Inviting..." : "Invite"}
                      </Button>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Invited users will be added once they log in
                    </p>
                  </div>
                </div>
                <DialogFooter>
                  <Button
                    variant="outline"
                    onClick={() => setCreateGroupOpen(false)}
                    disabled={isCreatingGroup}
                  >
                    Cancel
                  </Button>
                  <Button
                    onClick={handleCreateGroup}
                    disabled={
                      !groupName.trim() ||
                      selectedUserIds.length === 0 ||
                      isCreatingGroup
                    }
                  >
                    {isCreatingGroup ? "Creating..." : "Create Team"}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          ) : (
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <span>
                    <Button size="sm" disabled>
                      <Plus size={16} className="mr-2" />
                      Create Team
                    </Button>
                  </span>
                </TooltipTrigger>
                <TooltipContent>
                  <p>Only organization administrators can create teams.</p>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          )}

          {/* Edit Group Dialog */}
          {isOrgAdmin && (
            <Dialog open={editGroupOpen} onOpenChange={setEditGroupOpen}>
              <DialogContent className="sm:max-w-md">
                <DialogHeader>
                  <DialogTitle>Edit Team</DialogTitle>
                  <DialogDescription>
                    Update the team name and manage its members.
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="edit-group-name">Team Name</Label>
                    <Input
                      id="edit-group-name"
                      placeholder="Enter team name"
                      value={editGroupName}
                      onChange={(e) => setEditGroupName(e.target.value)}
                    />
                  </div>
                  <div className="space-y-3">
                    <Label>Team Members</Label>
                    <div className="space-y-2 max-h-48 overflow-y-auto">
                      {users.map((user) => (
                        <div
                          key={user.id}
                          className="flex items-center space-x-3"
                        >
                          <Checkbox
                            id={`edit-user-${user.id}`}
                            checked={editSelectedUserIds.includes(user.id)}
                            onCheckedChange={(checked) =>
                              handleEditUserSelection(
                                user.id,
                                checked as boolean
                              )
                            }
                          />
                          <div className="flex items-center space-x-2 flex-1">
                            {user.profilePicUrl ? (
                              <img
                                src={user.profilePicUrl}
                                alt={`${user.name}'s profile`}
                                className="w-6 h-6 rounded-full object-cover"
                              />
                            ) : (
                              <div className="w-6 h-6 rounded-full bg-muted flex items-center justify-center">
                                <UserIcon
                                  size={12}
                                  className="text-muted-foreground"
                                />
                              </div>
                            )}
                            <Label
                              htmlFor={`edit-user-${user.id}`}
                              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
                            >
                              {isInvitedPlaceholder(user) ? (
                                <span className="flex items-center gap-2">
                                  <span className="text-muted-foreground">
                                    {user.email}
                                  </span>
                                  <Badge
                                    variant="secondary"
                                    className="text-xs"
                                  >
                                    Pending
                                  </Badge>
                                </span>
                              ) : (
                                user.name
                              )}
                            </Label>
                          </div>
                        </div>
                      ))}
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {editSelectedUserIds.length} member
                      {editSelectedUserIds.length !== 1 ? "s" : ""} selected
                    </p>
                  </div>
                  <div className="space-y-2 pt-4 border-t">
                    <Label>Or invite by email</Label>
                    <div className="flex gap-2">
                      <Input
                        type="email"
                        placeholder="Enter email address"
                        value={editInviteEmail}
                        onChange={(e) => setEditInviteEmail(e.target.value)}
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            e.preventDefault();
                            handleEditInviteUser();
                          }
                        }}
                      />
                      <Button
                        type="button"
                        variant="outline"
                        onClick={handleEditInviteUser}
                        disabled={!editInviteEmail.trim() || isEditInviting}
                      >
                        {isEditInviting ? "Inviting..." : "Invite"}
                      </Button>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Invited users will be added once they log in
                    </p>
                  </div>
                </div>
                <DialogFooter>
                  <Button
                    variant="outline"
                    onClick={() => setEditGroupOpen(false)}
                    disabled={isUpdatingGroup}
                  >
                    Cancel
                  </Button>
                  <Button
                    onClick={handleUpdateGroup}
                    disabled={!editGroupName.trim() || isUpdatingGroup}
                  >
                    {isUpdatingGroup ? "Updating..." : "Update Team"}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          )}
        </div>

        {groupsLoading ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }, (_, i) => `skeleton-group-${i}`).map(
              (key) => (
                <Card key={key}>
                  <CardHeader>
                    <Skeleton className="h-4 w-3/4" />
                    <Skeleton className="h-3 w-1/2" />
                  </CardHeader>
                  <CardContent>
                    <Skeleton className="h-3 w-full" />
                  </CardContent>
                </Card>
              )
            )}
          </div>
        ) : groupsError ? (
          <Card>
            <CardContent className="py-10 text-center text-destructive">
              {groupsError}
            </CardContent>
          </Card>
        ) : groups.length === 0 ? (
          <Card>
            <CardContent className="py-10 text-center text-muted-foreground">
              No teams found
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {groups.map((group) => (
              <Card key={group.id}>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <CardTitle className="text-base font-medium">
                      {group.name}
                    </CardTitle>
                    {isOrgAdmin && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleEditGroup(group)}
                        className="h-8 w-8 p-0"
                      >
                        <Edit size={14} />
                      </Button>
                    )}
                  </div>
                </CardHeader>
                <CardContent>
                  {group.memberCount !== undefined && (
                    <div className="text-sm text-muted-foreground">
                      {group.memberCount} member
                      {group.memberCount !== 1 ? "s" : ""}
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

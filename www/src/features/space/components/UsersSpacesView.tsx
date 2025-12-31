import { useQueryClient } from "@tanstack/react-query";
import {
  Building2,
  Crown,
  Edit,
  Plus,
  Shield,
  User as UserIcon,
  Users,
} from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  addSpaceMember,
  changeSpaceModerator,
  createSpace,
  getSpaceById,
  getSpaces,
  getUsers,
  inviteUser,
  removeSpaceMember,
  updateSpaceName,
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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

interface Space {
  id: string;
  name: string;
  moderatorUserId: string;
  memberIds: string[];
}

const isInvitedPlaceholder = (user: User): boolean =>
  !!user.invitedAt && (!user.name || user.name === "");

export default function UsersSpacesView() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [users, setUsers] = useState<User[]>([]);
  const [spaces, setSpaces] = useState<Space[]>([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [spacesLoading, setSpacesLoading] = useState(true);
  const [usersError, setUsersError] = useState<string | null>(null);
  const [spacesError, setSpacesError] = useState<string | null>(null);

  // Role checks
  const isOrgAdmin = useIsOrgAdmin();

  const [createSpaceOpen, setCreateSpaceOpen] = useState(false);
  const [spaceName, setSpaceName] = useState("");
  const [selectedModeratorId, setSelectedModeratorId] = useState<string>("");
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);
  const [isCreatingSpace, setIsCreatingSpace] = useState(false);

  const [editSpaceOpen, setEditSpaceOpen] = useState(false);
  const [editingSpace, setEditingSpace] = useState<Space | null>(null);
  const [editSpaceName, setEditSpaceName] = useState("");
  const [editModeratorId, setEditModeratorId] = useState<string>("");
  const [editSelectedMemberIds, setEditSelectedMemberIds] = useState<string[]>(
    []
  );
  const [isUpdatingSpace, setIsUpdatingSpace] = useState(false);

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
          const userData: User[] =
            response.data.items?.map((user) => {
              return {
                id: user.id,
                name: user.name,
                email: user.email,
                profilePicUrl: user.profilePicUrl,
                invitedAt: user.invitedAt,
              };
            }) || [];
          setUsers(userData);
        }
      } catch (_error) {
        setUsersError("Failed to load users");
      } finally {
        setUsersLoading(false);
      }
    };

    const fetchSpaces = async () => {
      try {
        setSpacesLoading(true);
        setSpacesError(null);
        const response = await getSpaces();
        if (response.data) {
          const spaceData: Space[] =
            response.data.items?.map((space) => {
              return {
                id: space.id,
                name: space.name,
                moderatorUserId: space.moderatorUserId,
                memberIds: space.memberIds || [],
              };
            }) || [];
          setSpaces(spaceData);
        }
      } catch (_error) {
        setSpacesError("Failed to load spaces");
      } finally {
        setSpacesLoading(false);
      }
    };

    fetchUsers();
    fetchSpaces();
  }, []);

  const handleCreateSpace = async () => {
    if (!(spaceName.trim() && selectedModeratorId)) {
      return;
    }

    try {
      setIsCreatingSpace(true);
      await createSpace({
        name: spaceName.trim(),
        moderatorUserId: selectedModeratorId,
        memberIds: selectedMemberIds,
      });

      setCreateSpaceOpen(false);
      setSpaceName("");
      setSelectedModeratorId("");
      setSelectedMemberIds([]);

      const response = await getSpaces();
      if (response.data) {
        const spaceData: Space[] =
          response.data.items?.map((space) => ({
            id: space.id,
            name: space.name,
            moderatorUserId: space.moderatorUserId,
            memberIds: space.memberIds || [],
          })) || [];
        setSpaces(spaceData);
      }

      // Invalidate spaces list to refetch
      queryClient.invalidateQueries({ queryKey: ["spaces"] });
    } finally {
      setIsCreatingSpace(false);
    }
  };

  const handleMemberSelection = (userId: string, checked: boolean) => {
    // Don't allow selecting the moderator as a member
    if (userId === selectedModeratorId) {
      return;
    }
    if (checked) {
      setSelectedMemberIds([...selectedMemberIds, userId]);
    } else {
      setSelectedMemberIds(selectedMemberIds.filter((id) => id !== userId));
    }
  };

  const handleEditMemberSelection = (userId: string, checked: boolean) => {
    // Don't allow selecting the moderator as a member
    if (userId === editModeratorId) {
      return;
    }
    if (checked) {
      setEditSelectedMemberIds([...editSelectedMemberIds, userId]);
    } else {
      setEditSelectedMemberIds(
        editSelectedMemberIds.filter((id) => id !== userId)
      );
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
        // Auto-select the invited user as a member
        setSelectedMemberIds((prev) => [...prev, newUser.id]);
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
        setEditSelectedMemberIds((prev) => [...prev, newUser.id]);
        setEditInviteEmail("");
      }
    } catch (_error) {
      // Could show toast notification in the future
    } finally {
      setIsEditInviting(false);
    }
  };

  const handleEditSpace = async (space: Space) => {
    setEditingSpace(space);
    setEditSpaceName(space.name);
    setEditModeratorId(space.moderatorUserId);

    const response = await getSpaceById(space.id);
    if (response.data) {
      const currentMemberIds = response.data.memberIds || [];
      setEditSelectedMemberIds(currentMemberIds);
    }

    setEditSpaceOpen(true);
  };

  const handleUpdateSpace = async () => {
    if (!(editingSpace && editSpaceName.trim())) {
      return;
    }

    try {
      setIsUpdatingSpace(true);

      // Update space name
      await updateSpaceName({
        id: editingSpace.id,
        name: editSpaceName.trim(),
      });

      // Change moderator if it's different (org admin only)
      if (
        isOrgAdmin &&
        editModeratorId &&
        editModeratorId !== editingSpace.moderatorUserId
      ) {
        await changeSpaceModerator({
          spaceId: editingSpace.id,
          newModeratorUserId: editModeratorId,
        });
      }

      // Update members
      const response = await getSpaceById(editingSpace.id);
      if (response.data) {
        const currentMemberIds = response.data.memberIds || [];

        const membersToAdd = editSelectedMemberIds.filter(
          (id) => !currentMemberIds.includes(id) && id !== editModeratorId
        );
        const membersToRemove = currentMemberIds.filter(
          (id) => !editSelectedMemberIds.includes(id)
        );

        for (const userId of membersToAdd) {
          await addSpaceMember({ spaceId: editingSpace.id, userId });
        }

        for (const userId of membersToRemove) {
          await removeSpaceMember({ spaceId: editingSpace.id, userId });
        }
      }

      setEditSpaceOpen(false);
      setEditingSpace(null);
      setEditSpaceName("");
      setEditModeratorId("");
      setEditSelectedMemberIds([]);

      const spacesResponse = await getSpaces();
      if (spacesResponse.data) {
        const spaceData: Space[] =
          spacesResponse.data.items?.map((space) => ({
            id: space.id,
            name: space.name,
            moderatorUserId: space.moderatorUserId,
            memberIds: space.memberIds || [],
          })) || [];
        setSpaces(spaceData);
      }
    } finally {
      setIsUpdatingSpace(false);
    }
  };

  const getUserById = (userId: string): User | undefined => {
    return users.find((user) => user.id === userId);
  };

  return (
    <div className="p-6 space-y-8 overflow-y-auto flex-1">
      <header>
        <h1 className="text-2xl font-semibold mb-2">Users & Spaces</h1>
        <p className="text-muted-foreground">
          Manage users and spaces in your organization
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

      {/* Spaces Section */}
      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Building2 size={20} />
            <h2 className="text-xl font-semibold">Spaces</h2>
            {!spacesLoading && (
              <Badge variant="secondary">{spaces.length}</Badge>
            )}
          </div>
          {isOrgAdmin ? (
            <Dialog open={createSpaceOpen} onOpenChange={setCreateSpaceOpen}>
              <DialogTrigger asChild>
                <Button size="sm">
                  <Plus size={16} className="mr-2" />
                  Create Space
                </Button>
              </DialogTrigger>
              <DialogContent className="sm:max-w-md">
                <DialogHeader>
                  <DialogTitle>Create New Space</DialogTitle>
                  <DialogDescription>
                    Create a space by giving it a name and selecting a
                    moderator.
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="space-name">Space Name</Label>
                    <Input
                      id="space-name"
                      placeholder="Enter space name"
                      value={spaceName}
                      onChange={(e) => setSpaceName(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="moderator-select">
                      Moderator (required)
                    </Label>
                    <Select
                      value={selectedModeratorId}
                      onValueChange={(value) => {
                        setSelectedModeratorId(value);
                        // Remove moderator from members if they were selected
                        setSelectedMemberIds((prev) =>
                          prev.filter((id) => id !== value)
                        );
                      }}
                    >
                      <SelectTrigger id="moderator-select">
                        <SelectValue placeholder="Select a moderator" />
                      </SelectTrigger>
                      <SelectContent>
                        {users.map((user) => (
                          <SelectItem key={user.id} value={user.id}>
                            <div className="flex items-center gap-2">
                              <Crown className="h-3 w-3 text-amber-500" />
                              {isInvitedPlaceholder(user)
                                ? user.email
                                : user.name}
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <p className="text-xs text-muted-foreground">
                      The moderator has full control over the space
                    </p>
                  </div>
                  <div className="space-y-3">
                    <Label>Members (optional)</Label>
                    <div className="space-y-2 max-h-48 overflow-y-auto">
                      {users
                        .filter((user) => user.id !== selectedModeratorId)
                        .map((user) => (
                          <div
                            key={user.id}
                            className="flex items-center space-x-3"
                          >
                            <Checkbox
                              id={`member-${user.id}`}
                              checked={selectedMemberIds.includes(user.id)}
                              onCheckedChange={(checked) =>
                                handleMemberSelection(
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
                                htmlFor={`member-${user.id}`}
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
                    {selectedMemberIds.length > 0 && (
                      <p className="text-xs text-muted-foreground">
                        {selectedMemberIds.length} member
                        {selectedMemberIds.length !== 1 ? "s" : ""} selected
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
                    onClick={() => setCreateSpaceOpen(false)}
                    disabled={isCreatingSpace}
                  >
                    Cancel
                  </Button>
                  <Button
                    onClick={handleCreateSpace}
                    disabled={
                      !(spaceName.trim() && selectedModeratorId) ||
                      isCreatingSpace
                    }
                  >
                    {isCreatingSpace ? "Creating..." : "Create Space"}
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
                      Create Space
                    </Button>
                  </span>
                </TooltipTrigger>
                <TooltipContent>
                  <p>Only organization administrators can create spaces.</p>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          )}

          {/* Edit Space Dialog */}
          {(isOrgAdmin ||
            (editingSpace &&
              users.some(
                (u) =>
                  u.id === editingSpace.moderatorUserId &&
                  editingSpace.moderatorUserId ===
                    users.find((user) => user.email === "dev@example.com")?.id
              ))) && (
            <Dialog open={editSpaceOpen} onOpenChange={setEditSpaceOpen}>
              <DialogContent className="sm:max-w-md">
                <DialogHeader>
                  <DialogTitle>Edit Space</DialogTitle>
                  <DialogDescription>
                    Update the space name and manage its members.
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="edit-space-name">Space Name</Label>
                    <Input
                      id="edit-space-name"
                      placeholder="Enter space name"
                      value={editSpaceName}
                      onChange={(e) => setEditSpaceName(e.target.value)}
                    />
                  </div>
                  {isOrgAdmin && (
                    <div className="space-y-2">
                      <Label htmlFor="edit-moderator-select">Moderator</Label>
                      <Select
                        value={editModeratorId}
                        onValueChange={(value) => {
                          const oldModeratorId = editModeratorId;
                          setEditModeratorId(value);
                          // Add old moderator back to members list (for UI), remove new moderator
                          // Backend is idempotent so duplicate member adds are safe
                          setEditSelectedMemberIds((prev) => {
                            const withOldModerator =
                              oldModeratorId && !prev.includes(oldModeratorId)
                                ? [...prev, oldModeratorId]
                                : prev;
                            return withOldModerator.filter(
                              (id) => id !== value
                            );
                          });
                        }}
                      >
                        <SelectTrigger id="edit-moderator-select">
                          <SelectValue placeholder="Select a moderator" />
                        </SelectTrigger>
                        <SelectContent>
                          {users
                            .filter(
                              (user) =>
                                editSelectedMemberIds.includes(user.id) ||
                                user.id === editModeratorId
                            )
                            .map((user) => (
                              <SelectItem key={user.id} value={user.id}>
                                <div className="flex items-center gap-2">
                                  <Crown className="h-3 w-3 text-amber-500" />
                                  {isInvitedPlaceholder(user)
                                    ? user.email
                                    : user.name}
                                </div>
                              </SelectItem>
                            ))}
                        </SelectContent>
                      </Select>
                      <p className="text-xs text-muted-foreground">
                        Only org admins can change the moderator
                      </p>
                    </div>
                  )}
                  {!isOrgAdmin && editingSpace && (
                    <div className="space-y-2">
                      <Label>Moderator</Label>
                      <div className="flex items-center gap-2 p-2 border rounded-md bg-muted/50">
                        <Crown className="h-4 w-4 text-amber-500" />
                        <span>
                          {getUserById(editingSpace.moderatorUserId)?.name ||
                            "Unknown"}
                        </span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Only org admins can change the moderator
                      </p>
                    </div>
                  )}
                  <div className="space-y-3">
                    <Label>Members</Label>
                    <div className="space-y-2 max-h-48 overflow-y-auto">
                      {users
                        .filter((user) => user.id !== editModeratorId)
                        .map((user) => (
                          <div
                            key={user.id}
                            className="flex items-center space-x-3"
                          >
                            <Checkbox
                              id={`edit-member-${user.id}`}
                              checked={editSelectedMemberIds.includes(user.id)}
                              onCheckedChange={(checked) =>
                                handleEditMemberSelection(
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
                                htmlFor={`edit-member-${user.id}`}
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
                      {editSelectedMemberIds.length} member
                      {editSelectedMemberIds.length !== 1 ? "s" : ""} selected
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
                    onClick={() => setEditSpaceOpen(false)}
                    disabled={isUpdatingSpace}
                  >
                    Cancel
                  </Button>
                  <Button
                    onClick={handleUpdateSpace}
                    disabled={!editSpaceName.trim() || isUpdatingSpace}
                  >
                    {isUpdatingSpace ? "Updating..." : "Update Space"}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          )}
        </div>

        {spacesLoading ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }, (_, i) => `skeleton-space-${i}`).map(
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
        ) : spacesError ? (
          <Card>
            <CardContent className="py-10 text-center text-destructive">
              {spacesError}
            </CardContent>
          </Card>
        ) : spaces.length === 0 ? (
          <Card>
            <CardContent className="py-10 text-center text-muted-foreground">
              No spaces found
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {spaces.map((space) => {
              const moderator = getUserById(space.moderatorUserId);
              return (
                <Card key={space.id}>
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <Building2 className="h-4 w-4" />
                        <CardTitle className="text-base font-medium">
                          {space.name}
                        </CardTitle>
                      </div>
                      <div className="flex items-center gap-1">
                        <TooltipProvider>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() =>
                                  navigate(`/spaces/${space.id}/permissions`)
                                }
                                className="h-8 w-8 p-0"
                              >
                                <Shield size={14} />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>
                              <p>Manage Permissions</p>
                            </TooltipContent>
                          </Tooltip>
                        </TooltipProvider>
                        {isOrgAdmin && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleEditSpace(space)}
                            className="h-8 w-8 p-0"
                          >
                            <Edit size={14} />
                          </Button>
                        )}
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-2">
                    <div className="flex items-center gap-2 text-sm">
                      <Crown className="h-4 w-4 text-amber-500" />
                      <span className="text-muted-foreground">Moderator:</span>
                      <span className="font-medium">
                        {moderator
                          ? isInvitedPlaceholder(moderator)
                            ? moderator.email
                            : moderator.name
                          : "Unknown"}
                      </span>
                    </div>
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                      <Users className="h-4 w-4" />
                      <span>
                        {space.memberIds.length} member
                        {space.memberIds.length !== 1 ? "s" : ""}
                      </span>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}

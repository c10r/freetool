import { Crown, GlobeLock, User as UserIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { getSpaces, getUsers } from "@/api/api";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
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
import { compareUsersByName } from "@/lib/utils";

interface User {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
  invitedAt?: string;
  isOrgAdmin?: boolean;
}

interface Space {
  id: string;
  name: string;
  moderatorUserId: string;
  memberIds: string[];
}

interface UserSpace {
  id: string;
  name: string;
  isModerator: boolean;
}

interface UserWithSpaces extends User {
  spaces: UserSpace[];
}

const isInvitedPlaceholder = (user: User): boolean =>
  !!user.invitedAt && (!user.name || user.name === "");

/**
 * Build a mapping of users with their associated spaces.
 * Spaces are sorted: moderated spaces first, then alphabetically.
 */
function buildUserSpacesMap(users: User[], spaces: Space[]): UserWithSpaces[] {
  return users.map((user) => ({
    ...user,
    spaces: spaces
      .filter(
        (space) =>
          space.moderatorUserId === user.id || space.memberIds.includes(user.id)
      )
      .map((space) => ({
        id: space.id,
        name: space.name,
        isModerator: space.moderatorUserId === user.id,
      }))
      .sort((a, b) => {
        // Moderator spaces first, then alphabetical
        if (a.isModerator !== b.isModerator) {
          return a.isModerator ? -1 : 1;
        }
        return a.name.localeCompare(b.name);
      }),
  }));
}

export default function UsersView() {
  const [users, setUsers] = useState<User[]>([]);
  const [spaces, setSpaces] = useState<Space[]>([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [spacesLoading, setSpacesLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        setUsersLoading(true);
        const response = await getUsers();
        if (response.data) {
          const userData: User[] =
            response.data.items?.map((user) => ({
              id: user.id,
              name: user.name,
              email: user.email,
              profilePicUrl: user.profilePicUrl,
              invitedAt: user.invitedAt,
              isOrgAdmin: user.isOrgAdmin,
            })) || [];
          setUsers(userData);
        }
      } catch (_error) {
        setError("Failed to load users");
      } finally {
        setUsersLoading(false);
      }
    };

    const fetchSpaces = async () => {
      try {
        setSpacesLoading(true);
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
      } catch (_error) {
        setError("Failed to load spaces");
      } finally {
        setSpacesLoading(false);
      }
    };

    fetchUsers();
    fetchSpaces();
  }, []);

  const usersWithSpaces = useMemo(
    () => buildUserSpacesMap(users, spaces).sort(compareUsersByName),
    [users, spaces]
  );

  const isLoading = usersLoading || spacesLoading;

  if (isLoading) {
    return (
      <div className="p-6 space-y-6 overflow-y-auto flex-1">
        <header>
          <h1 className="text-2xl font-semibold mb-2">Users</h1>
          <p className="text-muted-foreground">
            View all users in your organization
          </p>
        </header>

        <Card>
          <CardContent className="p-0">
            <div className="p-4 space-y-4">
              {Array.from({ length: 5 }, (_, i) => `skeleton-${i}`).map(
                (key) => (
                  <div key={key} className="flex items-center gap-4">
                    <Skeleton className="h-10 w-10 rounded-full" />
                    <div className="space-y-2 flex-1">
                      <Skeleton className="h-4 w-1/4" />
                      <Skeleton className="h-3 w-1/3" />
                    </div>
                  </div>
                )
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6 space-y-6 overflow-y-auto flex-1">
        <header>
          <h1 className="text-2xl font-semibold mb-2">Users</h1>
          <p className="text-muted-foreground">
            View all users in your organization
          </p>
        </header>

        <Card>
          <CardContent className="py-10 text-center text-destructive">
            {error}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (users.length === 0) {
    return (
      <div className="p-6 space-y-6 overflow-y-auto flex-1">
        <header>
          <h1 className="text-2xl font-semibold mb-2">Users</h1>
          <p className="text-muted-foreground">
            View all users in your organization
          </p>
        </header>

        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No users found
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 overflow-y-auto flex-1">
      <header>
        <div className="flex items-center gap-2 mb-2">
          <h1 className="text-2xl font-semibold">Users</h1>
          <Badge variant="secondary">{users.length}</Badge>
        </div>
        <p className="text-muted-foreground">
          View all users in your organization
        </p>
      </header>

      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12" />
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Badge</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Spaces</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {usersWithSpaces.map((user) => (
                  <TableRow key={user.id}>
                    {/* Avatar */}
                    <TableCell>
                      <Avatar className="h-10 w-10">
                        <AvatarImage
                          src={user.profilePicUrl}
                          alt={user.name || user.email}
                        />
                        <AvatarFallback>
                          <UserIcon
                            size={20}
                            className="text-muted-foreground"
                          />
                        </AvatarFallback>
                      </Avatar>
                    </TableCell>

                    {/* Name */}
                    <TableCell className="font-medium">
                      {isInvitedPlaceholder(user) ? (
                        <span className="text-muted-foreground italic">
                          Invited
                        </span>
                      ) : (
                        user.name
                      )}
                    </TableCell>

                    {/* Email */}
                    <TableCell className="text-muted-foreground">
                      {user.email}
                    </TableCell>

                    {/* Badge (Org Admin only) */}
                    <TableCell>
                      {user.isOrgAdmin && (
                        <TooltipProvider>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Badge
                                variant="secondary"
                                className="gap-1 text-blue-600 bg-blue-50 hover:bg-blue-100"
                              >
                                <GlobeLock className="h-3 w-3" />
                                Org Admin
                              </Badge>
                            </TooltipTrigger>
                            <TooltipContent>
                              <p>Has full access to all spaces</p>
                            </TooltipContent>
                          </Tooltip>
                        </TooltipProvider>
                      )}
                    </TableCell>

                    {/* Status (Pending or none) */}
                    <TableCell>
                      {isInvitedPlaceholder(user) && (
                        <Badge variant="outline" className="text-amber-600">
                          Pending
                        </Badge>
                      )}
                    </TableCell>

                    {/* Spaces */}
                    <TableCell>
                      {user.spaces.length === 0 ? (
                        <span className="text-muted-foreground">-</span>
                      ) : (
                        <div className="flex flex-wrap gap-1.5">
                          {user.spaces.map((space, index) => (
                            <span
                              key={space.id}
                              className="inline-flex items-center"
                            >
                              {space.isModerator && (
                                <TooltipProvider>
                                  <Tooltip>
                                    <TooltipTrigger asChild>
                                      <Crown className="h-3 w-3 text-amber-500 mr-0.5" />
                                    </TooltipTrigger>
                                    <TooltipContent>
                                      <p>Moderator of {space.name}</p>
                                    </TooltipContent>
                                  </Tooltip>
                                </TooltipProvider>
                              )}
                              <span className="text-sm">
                                {space.name}
                                {index < user.spaces.length - 1 && ","}
                              </span>
                            </span>
                          ))}
                        </div>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

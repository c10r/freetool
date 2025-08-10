import { useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Users, User } from "lucide-react";
import { getUsers, getGroups } from "@/api/api";

interface User {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
}

interface Group {
  id: string;
  name: string;
  memberCount?: number;
}

export default function UsersTeamsView() {
  const [users, setUsers] = useState<User[]>([]);
  const [groups, setGroups] = useState<Group[]>([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [groupsLoading, setGroupsLoading] = useState(true);
  const [usersError, setUsersError] = useState<string | null>(null);
  const [groupsError, setGroupsError] = useState<string | null>(null);

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        setUsersLoading(true);
        setUsersError(null);
        const response = await getUsers();
        if (response.data) {
          const userData: User[] = response.data.items?.map((user) => {
            return {
              id: user.id as string,
              name: user.name,
              email: user.email,
              profilePicUrl: user.profilePicUrl as string,
            };
          });
          setUsers(userData);
        }
      } catch (error) {
        setUsersError("Failed to load users");
        console.error("Error fetching users:", error);
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
              id: group.id as string,
              name: group.name,
              memberCount: group.userIds?.length || 0,
            };
          });
          setGroups(groupData);
        }
      } catch (error) {
        setGroupsError("Failed to load teams");
        console.error("Error fetching groups:", error);
      } finally {
        setGroupsLoading(false);
      }
    };

    fetchUsers();
    fetchGroups();
  }, []);

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
          <User size={20} />
          <h2 className="text-xl font-semibold">Users</h2>
          {!usersLoading && <Badge variant="secondary">{users.length}</Badge>}
        </div>

        {usersLoading ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Card key={i}>
                <CardHeader>
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/2" />
                </CardHeader>
                <CardContent>
                  <Skeleton className="h-3 w-full" />
                </CardContent>
              </Card>
            ))}
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
              <Card key={user.id}>
                <CardHeader>
                  <CardTitle className="text-base font-medium">
                    {user.name}
                  </CardTitle>
                  <p className="text-sm text-muted-foreground">{user.email}</p>
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
                      <User size={20} className="text-muted-foreground" />
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
        <div className="flex items-center gap-2">
          <Users size={20} />
          <h2 className="text-xl font-semibold">Teams</h2>
          {!groupsLoading && <Badge variant="secondary">{groups.length}</Badge>}
        </div>

        {groupsLoading ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Card key={i}>
                <CardHeader>
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/2" />
                </CardHeader>
                <CardContent>
                  <Skeleton className="h-3 w-full" />
                </CardContent>
              </Card>
            ))}
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
                  <CardTitle className="text-base font-medium">
                    {group.name}
                  </CardTitle>
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

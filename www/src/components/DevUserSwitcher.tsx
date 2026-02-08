import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import {
  getDevUserId,
  getDevUsers,
  isDevModeEnabled,
  setDevUserId,
} from "@/api/api";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

export function DevUserSwitcher() {
  if (!isDevModeEnabled()) {
    return null;
  }

  return <DevUserSwitcherContent />;
}

function DevUserSwitcherContent() {
  const {
    data: users,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["devUsers"],
    queryFn: getDevUsers,
  });

  const currentUserId = getDevUserId();

  // Auto-select first user if none selected
  useEffect(() => {
    if (users && users.length > 0 && !currentUserId) {
      setDevUserId(users[0].id);
      window.location.href = "/";
    }
  }, [users, currentUserId]);

  const handleUserChange = (userId: string) => {
    setDevUserId(userId);
    window.location.href = "/";
  };

  if (isLoading || (!currentUserId && users)) {
    return (
      <div className="text-sm text-muted-foreground">Loading users...</div>
    );
  }

  if (error || !users) {
    return <div className="text-sm text-red-500">Failed to load users</div>;
  }

  const selectedUser = users.find((u) => u.id === currentUserId);

  return (
    <Select value={currentUserId || ""} onValueChange={handleUserChange}>
      <SelectTrigger className="w-[200px]">
        <SelectValue placeholder="Select a user">
          {selectedUser ? selectedUser.name : "Select a user"}
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        {users.map((user) => (
          <SelectItem key={user.id} value={user.id}>
            <div className="flex flex-col">
              <span>{user.name}</span>
              <span className="text-xs text-muted-foreground">
                {user.email}
              </span>
            </div>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

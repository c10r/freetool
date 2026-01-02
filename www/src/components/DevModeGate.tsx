import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import {
  getDevUserId,
  getDevUsers,
  isDevModeEnabled,
  setDevUserId,
} from "@/api/api";

interface DevModeGateProps {
  children: React.ReactNode;
}

/**
 * Gate component that blocks the app from rendering until a dev user is selected.
 * In production mode, this component is transparent and renders children immediately.
 */
export function DevModeGate({ children }: DevModeGateProps) {
  console.log("[DEV] DevModeGate: rendering");

  // If not dev mode, render children immediately
  const devModeEnabled = isDevModeEnabled();
  console.log("[DEV] DevModeGate: devModeEnabled =", devModeEnabled);
  if (!devModeEnabled) {
    console.log("[DEV] DevModeGate: NOT dev mode, rendering children");
    return <>{children}</>;
  }

  // If user already selected, render children
  const userId = getDevUserId();
  console.log("[DEV] DevModeGate: userId =", userId);
  if (userId) {
    console.log("[DEV] DevModeGate: user selected, rendering children");
    return <>{children}</>;
  }

  // Otherwise, show the user selector
  console.log("[DEV] DevModeGate: NO user selected, showing selector");
  return <DevUserSelector />;
}

/**
 * Component that fetches users and auto-selects the first one.
 * Shows a loading screen while fetching.
 */
function DevUserSelector() {
  console.log("[DEV] DevUserSelector: rendering");

  const {
    data: users,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["devUsers"],
    queryFn: getDevUsers,
  });

  console.log("[DEV] DevUserSelector: query state", {
    isLoading,
    error,
    users,
  });

  // Auto-select first user when users are loaded
  useEffect(() => {
    console.log("[DEV] DevUserSelector useEffect: users =", users);
    if (users && users.length > 0) {
      console.log(
        "[DEV] DevUserSelector: auto-selecting first user:",
        users[0]
      );
      setDevUserId(users[0].id);
      console.log("[DEV] DevUserSelector: navigating to /freetool...");
      window.location.href = "/freetool";
    }
  }, [users]);

  if (error) {
    console.log("[DEV] DevUserSelector: rendering error state", error);
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <h1 className="text-xl font-semibold text-destructive mb-2">
            Failed to load dev users
          </h1>
          <p className="text-muted-foreground">
            Make sure the backend is running and dev mode is enabled.
          </p>
          <p className="text-sm text-muted-foreground mt-2">
            {error instanceof Error ? error.message : "Unknown error"}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-screen w-screen flex items-center justify-center bg-background">
      <div className="text-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-4" />
        <h1 className="text-xl font-semibold mb-2">Development Mode</h1>
        <p className="text-muted-foreground">
          {isLoading ? "Loading users..." : "Selecting user..."}
        </p>
      </div>
    </div>
  );
}

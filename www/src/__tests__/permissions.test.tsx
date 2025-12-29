/**
 * Tests for Permission Infrastructure
 *
 * NOTE: These tests require a testing framework (Vitest/Jest) to be configured.
 * Run: npm install -D vitest @testing-library/react @testing-library/jest-dom
 *
 * To run tests: npm test
 */

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as api from "@/api/api";
import { PermissionButton } from "@/components/PermissionButton";
import { PermissionGate } from "@/components/PermissionGate";
import { AuthorizationProvider } from "@/contexts/AuthorizationContext";
import { useAuthorization } from "@/hooks/useAuthorization";
import { useHasPermission } from "@/hooks/usePermissions";

// Mock the API module
vi.mock("@/api/api");

/**
 * Test helper to wrap components with providers
 */
function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthorizationProvider>{ui}</AuthorizationProvider>
    </QueryClientProvider>
  );
}

describe("AuthorizationContext", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should fetch current user on mount", async () => {
    const mockUser = {
      id: "user-1",
      name: "Test User",
      email: "test@example.com",
      isOrgAdmin: false,
      teams: [],
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({ data: mockUser });

    function TestComponent() {
      const { currentUser } = useAuthorization();
      return <div>{currentUser?.name || "Loading..."}</div>;
    }

    renderWithProviders(<TestComponent />);

    await waitFor(() => {
      expect(screen.getByText("Test User")).toBeInTheDocument();
    });
  });

  it("should return correct org admin status", async () => {
    const mockUser = {
      id: "user-1",
      name: "Admin User",
      email: "admin@example.com",
      isOrgAdmin: true,
      teams: [],
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({ data: mockUser });

    function TestComponent() {
      const { isOrgAdmin } = useAuthorization();
      return <div>{isOrgAdmin() ? "Is Admin" : "Not Admin"}</div>;
    }

    renderWithProviders(<TestComponent />);

    await waitFor(() => {
      expect(screen.getByText("Is Admin")).toBeInTheDocument();
    });
  });
});

describe("useHasPermission hook", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should return true when user has permission", async () => {
    const mockPermissions = {
      spaceId: "ws-1",
      userId: "user-1",
      permissions: {
        create_app: true,
        edit_app: false,
        delete_app: false,
        create_resource: false,
        edit_resource: false,
        delete_resource: false,
        run_app: true,
        create_folder: false,
        edit_folder: false,
        delete_folder: false,
      },
      isOrgAdmin: false,
      isSpaceModerator: false,
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({
      data: {
        id: "user-1",
        name: "Test User",
        email: "test@example.com",
        isOrgAdmin: false,
        teams: [],
      },
    });

    vi.mocked(api.getSpacePermissions).mockResolvedValue({
      data: mockPermissions,
    });

    function TestComponent() {
      const canCreate = useHasPermission("ws-1", "create_app");
      const canEdit = useHasPermission("ws-1", "edit_app");

      return (
        <div>
          <div>{canCreate ? "Can Create" : "Cannot Create"}</div>
          <div>{canEdit ? "Can Edit" : "Cannot Edit"}</div>
        </div>
      );
    }

    renderWithProviders(<TestComponent />);

    await waitFor(() => {
      expect(screen.getByText("Can Create")).toBeInTheDocument();
      expect(screen.getByText("Cannot Edit")).toBeInTheDocument();
    });
  });
});

describe("PermissionGate component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should render children when user has permission", async () => {
    const mockPermissions = {
      spaceId: "ws-1",
      userId: "user-1",
      permissions: {
        create_app: true,
        edit_app: true,
        delete_app: true,
        create_resource: true,
        edit_resource: true,
        delete_resource: true,
        run_app: true,
        create_folder: true,
        edit_folder: true,
        delete_folder: true,
      },
      isOrgAdmin: true,
      isSpaceModerator: true,
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({
      data: {
        id: "user-1",
        name: "Admin",
        email: "admin@example.com",
        isOrgAdmin: true,
        teams: [],
      },
    });

    vi.mocked(api.getSpacePermissions).mockResolvedValue({
      data: mockPermissions,
    });

    renderWithProviders(
      <PermissionGate spaceId="ws-1" permission="create_app">
        <button type="button">Create App</button>
      </PermissionGate>
    );

    await waitFor(() => {
      expect(screen.getByText("Create App")).toBeInTheDocument();
    });
  });

  it("should hide children when user lacks permission", async () => {
    const mockPermissions = {
      spaceId: "ws-1",
      userId: "user-1",
      permissions: {
        create_app: false,
        edit_app: false,
        delete_app: false,
        create_resource: false,
        edit_resource: false,
        delete_resource: false,
        run_app: false,
        create_folder: false,
        edit_folder: false,
        delete_folder: false,
      },
      isOrgAdmin: false,
      isSpaceModerator: false,
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({
      data: {
        id: "user-1",
        name: "User",
        email: "user@example.com",
        isOrgAdmin: false,
        teams: [],
      },
    });

    vi.mocked(api.getSpacePermissions).mockResolvedValue({
      data: mockPermissions,
    });

    renderWithProviders(
      <PermissionGate spaceId="ws-1" permission="create_app">
        <button type="button">Create App</button>
      </PermissionGate>
    );

    await waitFor(() => {
      expect(screen.queryByText("Create App")).not.toBeInTheDocument();
    });
  });

  it("should render fallback when user lacks permission", async () => {
    const mockPermissions = {
      spaceId: "ws-1",
      userId: "user-1",
      permissions: {
        create_app: false,
        edit_app: false,
        delete_app: false,
        create_resource: false,
        edit_resource: false,
        delete_resource: false,
        run_app: false,
        create_folder: false,
        edit_folder: false,
        delete_folder: false,
      },
      isOrgAdmin: false,
      isSpaceModerator: false,
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({
      data: {
        id: "user-1",
        name: "User",
        email: "user@example.com",
        isOrgAdmin: false,
        teams: [],
      },
    });

    vi.mocked(api.getSpacePermissions).mockResolvedValue({
      data: mockPermissions,
    });

    renderWithProviders(
      <PermissionGate
        spaceId="ws-1"
        permission="create_app"
        fallback={<div>No Permission</div>}
      >
        <button type="button">Create App</button>
      </PermissionGate>
    );

    await waitFor(() => {
      expect(screen.getByText("No Permission")).toBeInTheDocument();
      expect(screen.queryByText("Create App")).not.toBeInTheDocument();
    });
  });
});

describe("PermissionButton component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should be enabled when user has permission", async () => {
    const mockPermissions = {
      spaceId: "ws-1",
      userId: "user-1",
      permissions: {
        create_app: true,
        edit_app: true,
        delete_app: true,
        create_resource: true,
        edit_resource: true,
        delete_resource: true,
        run_app: true,
        create_folder: true,
        edit_folder: true,
        delete_folder: true,
      },
      isOrgAdmin: true,
      isSpaceModerator: true,
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({
      data: {
        id: "user-1",
        name: "Admin",
        email: "admin@example.com",
        isOrgAdmin: true,
        teams: [],
      },
    });

    vi.mocked(api.getSpacePermissions).mockResolvedValue({
      data: mockPermissions,
    });

    renderWithProviders(
      <PermissionButton spaceId="ws-1" permission="delete_app">
        Delete
      </PermissionButton>
    );

    await waitFor(() => {
      const button = screen.getByText("Delete");
      expect(button).toBeEnabled();
    });
  });

  it("should be disabled when user lacks permission", async () => {
    const mockPermissions = {
      spaceId: "ws-1",
      userId: "user-1",
      permissions: {
        create_app: false,
        edit_app: false,
        delete_app: false,
        create_resource: false,
        edit_resource: false,
        delete_resource: false,
        run_app: false,
        create_folder: false,
        edit_folder: false,
        delete_folder: false,
      },
      isOrgAdmin: false,
      isSpaceModerator: false,
    };

    vi.mocked(api.getCurrentUser).mockResolvedValue({
      data: {
        id: "user-1",
        name: "User",
        email: "user@example.com",
        isOrgAdmin: false,
        teams: [],
      },
    });

    vi.mocked(api.getSpacePermissions).mockResolvedValue({
      data: mockPermissions,
    });

    renderWithProviders(
      <PermissionButton spaceId="ws-1" permission="delete_app">
        Delete
      </PermissionButton>
    );

    await waitFor(() => {
      const button = screen.getByText("Delete");
      expect(button).toBeDisabled();
    });
  });
});

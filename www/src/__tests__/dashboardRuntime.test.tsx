import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as dashboardApi from "@/api/api";
import DashboardView from "@/features/space/components/DashboardView";
import type {
  DashboardNode,
  FolderNode,
  SpaceMainProps,
} from "@/features/space/types";
import type { Permission } from "@/types/permissions";

vi.mock("@/api/api");

const mockUseHasPermission = vi.fn();

vi.mock("@/hooks/usePermissions", () => ({
  useHasPermission: (spaceId: string, permission: Permission) =>
    mockUseHasPermission(spaceId, permission),
}));

const createDashboardConfiguration = (overrides?: Record<string, unknown>) =>
  JSON.stringify(
    {
      loadInputs: [
        {
          id: "load-customer-id",
          label: "Customer ID",
          type: "text",
          required: true,
        },
      ],
      actionInputs: [
        {
          id: "action-comment",
          label: "Comment",
          type: "text",
          required: false,
        },
      ],
      actions: [
        {
          id: "approve-action",
          appId: "app-approve",
          label: "Approve",
          section: "center",
        },
        {
          id: "reject-action",
          appId: "app-reject",
          label: "Reject",
          section: "center",
        },
      ],
      bindings: [],
      layout: {
        left: [],
        center: ["approve-action", "reject-action"],
        right: [],
      },
      ...overrides,
    },
    null,
    2
  );

const createProps = (dashboardOverrides?: Partial<DashboardNode>) => {
  const folder: FolderNode = {
    id: "folder-1",
    name: "Folder",
    type: "folder",
    childrenIds: ["dashboard-1", "app-approve", "app-reject"],
    spaceId: "space-1",
  };

  const dashboard: DashboardNode = {
    id: "dashboard-1",
    name: "Operations Dashboard",
    type: "dashboard",
    parentId: "folder-1",
    prepareAppId: "app-prepare",
    configuration: createDashboardConfiguration(),
    ...dashboardOverrides,
  };

  const nodes: SpaceMainProps["nodes"] = {
    "folder-1": folder,
    "dashboard-1": dashboard,
    "app-prepare": {
      id: "app-prepare",
      name: "Prepare App",
      type: "app",
      parentId: "folder-1",
      fields: [],
      resourceId: "resource-1",
    },
    "app-approve": {
      id: "app-approve",
      name: "Approve App",
      type: "app",
      parentId: "folder-1",
      fields: [],
      resourceId: "resource-1",
    },
    "app-reject": {
      id: "app-reject",
      name: "Reject App",
      type: "app",
      parentId: "folder-1",
      fields: [],
      resourceId: "resource-1",
    },
  };

  const props: SpaceMainProps & { dashboard: DashboardNode } = {
    dashboard,
    nodes,
    selectedId: dashboard.id,
    onSelect: vi.fn(),
    updateNode: vi.fn(),
    insertFolderNode: vi.fn(),
    createFolder: vi.fn(),
    createApp: vi.fn(() => Promise.resolve()),
    createDashboard: vi.fn(() => Promise.resolve()),
    deleteNode: vi.fn(),
    endpoints: {},
    createEndpoint: vi.fn(),
    updateEndpoint: vi.fn(),
    deleteEndpoint: vi.fn(),
    spaceId: "space-1",
    spaceName: "Operations",
  };

  return props;
};

const renderDashboardRuntime = (
  props: SpaceMainProps & { dashboard: DashboardNode }
) => {
  render(
    <MemoryRouter>
      <DashboardView {...props} mode="run" />
    </MemoryRouter>
  );
};

describe("Dashboard runtime UX", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockUseHasPermission.mockImplementation(
      (_spaceId: string, permission: Permission) => {
        if (permission === "run_dashboard" || permission === "run_app") {
          return true;
        }
        return true;
      }
    );

    vi.mocked(dashboardApi.prepareDashboard).mockResolvedValue({
      data: {
        prepareRunId: "prepare-run-1",
        status: "Success",
        response: '{"prepared":true}',
        errorMessage: null,
      },
    });

    vi.mocked(dashboardApi.runDashboardAction).mockResolvedValue({
      data: {
        actionRunId: "action-run-1",
        status: "Success",
        response: '{"ok":true}',
        errorMessage: null,
      },
    });
  });

  it("shows runtime read-only warning when run permission is missing", () => {
    mockUseHasPermission.mockImplementation(
      (_spaceId: string, permission: Permission) => {
        if (permission === "run_dashboard") {
          return false;
        }
        return true;
      }
    );

    const props = createProps();
    renderDashboardRuntime(props);

    expect(screen.getByText(/read-only at runtime/i)).toBeInTheDocument();
  });

  it("freezes load inputs after prepare and re-enables them after reset", async () => {
    const props = createProps();
    renderDashboardRuntime(props);

    const loadInput = screen.getByLabelText("Customer ID *");
    expect(loadInput).toBeEnabled();

    fireEvent.change(loadInput, { target: { value: "C-100" } });
    fireEvent.click(screen.getByRole("button", { name: "Load Dashboard" }));

    await waitFor(() => {
      expect(screen.getByText("Post-load")).toBeInTheDocument();
    });

    expect(loadInput).toBeDisabled();

    fireEvent.click(screen.getByRole("button", { name: "Reset" }));
    const resetDialog = await screen.findByRole("alertdialog");
    fireEvent.click(within(resetDialog).getByRole("button", { name: "Reset" }));

    await waitFor(() => {
      expect(screen.getByText("Pre-load")).toBeInTheDocument();
    });

    expect(loadInput).toBeEnabled();
  });

  it("locks all action buttons while an action is running", async () => {
    let resolveRun:
      | ((
          value: Awaited<ReturnType<typeof dashboardApi.runDashboardAction>>
        ) => void)
      | null = null;

    vi.mocked(dashboardApi.runDashboardAction).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveRun = resolve;
        })
    );

    const props = createProps({ prepareAppId: undefined });
    renderDashboardRuntime(props);

    fireEvent.change(screen.getByLabelText("Customer ID *"), {
      target: { value: "C-200" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Load Dashboard" }));

    await waitFor(() => {
      expect(screen.getByText("Post-load")).toBeInTheDocument();
    });

    const approveButton = screen.getByRole("button", { name: "Approve" });
    const rejectButton = screen.getByRole("button", { name: "Reject" });

    fireEvent.click(approveButton);

    await waitFor(() => {
      expect(approveButton).toBeDisabled();
      expect(rejectButton).toBeDisabled();
    });

    resolveRun?.({
      data: {
        actionRunId: "action-run-2",
        status: "Success",
        response: '{"ok":true}',
        errorMessage: null,
      },
    });

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Approve" })).toBeEnabled();
      expect(screen.getByRole("button", { name: "Reject" })).toBeEnabled();
    });
  });
});

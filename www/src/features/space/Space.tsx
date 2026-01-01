import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { createApp as createAppAPI } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { AuthorizationProvider } from "@/contexts/AuthorizationContext";
import {
  useHasAnyPermission,
  useIsOrgAdmin,
  useSpacePermissions,
} from "@/hooks/usePermissions";
import { useSidebarTree } from "@/hooks/useSidebarTree";
import { useSpace } from "@/hooks/useSpaces";
import { toBackendInputType } from "@/lib/inputTypeMapper";
import NotFound from "@/pages/NotFound";
import type { SpaceWithDetails } from "@/types/space";
import SidebarTree from "./SidebarTree";
import SpaceMain from "./SpaceMain";
import type {
  AppField,
  Endpoint,
  FolderNode,
  KeyValuePair,
  SpaceNode,
} from "./types";

// Helper functions to convert between selectedId and URL paths
function getPathFromSelectedId(
  selectedId: string,
  nodes: Record<string, SpaceNode>,
  spaceId?: string
): string {
  if (selectedId === "users") {
    return "/users";
  }
  if (selectedId === "spaces-list") {
    return "/spaces-list";
  }
  if (selectedId === "audit-log") {
    return "/audit";
  }
  if (selectedId === "resources") {
    // Resources are now space-specific
    return spaceId ? `/spaces/${spaceId}/resources` : "/spaces";
  }
  if (selectedId === "trash") {
    // Trash is space-specific
    return spaceId ? `/spaces/${spaceId}/trash` : "/spaces";
  }
  if (selectedId === "settings" || selectedId === "permissions") {
    // Settings (formerly permissions) are space-specific
    return spaceId ? `/spaces/${spaceId}/settings` : "/spaces";
  }
  if (selectedId === "root") {
    return spaceId ? `/spaces/${spaceId}` : "/spaces";
  }

  // For space nodes, use /spaces/:spaceId/:nodeId
  if (nodes[selectedId] && spaceId) {
    return `/spaces/${spaceId}/${selectedId}`;
  }

  return spaceId ? `/spaces/${spaceId}` : "/spaces";
}

function getSelectedIdFromPath(
  pathname: string,
  spaceId?: string,
  nodeId?: string,
  rootId = "root"
): string {
  if (pathname === "/users") {
    return "users";
  }
  if (pathname === "/spaces-list") {
    return "spaces-list";
  }
  if (pathname === "/audit") {
    return "audit-log";
  }
  if (pathname === `/spaces/${spaceId}/resources`) {
    return "resources";
  }
  if (pathname === `/spaces/${spaceId}/trash`) {
    return "trash";
  }
  if (pathname === `/spaces/${spaceId}/settings`) {
    return "settings";
  }
  // Backwards compatibility: old permissions URL maps to settings
  if (pathname === `/spaces/${spaceId}/permissions`) {
    return "permissions";
  }
  if (pathname === "/spaces" && !spaceId) {
    return rootId;
  }
  if (pathname === `/spaces/${spaceId}` && !nodeId) {
    return rootId;
  }
  if (pathname.startsWith("/spaces/") && nodeId) {
    return nodeId;
  }
  if (pathname === "/") {
    return rootId;
  }

  return rootId;
}

// Inner component that uses space permissions
function WorkspaceContent() {
  const { spaceId, nodeId } = useParams<{
    spaceId?: string;
    nodeId?: string;
  }>();
  const location = useLocation();
  const navigate = useNavigate();

  // Fetch all spaces, folders, and apps upfront using the sidebar tree hook
  const {
    nodes,
    spaces,
    rootId,
    isLoading: loadingTree,
    error: treeError,
    invalidateTree,
  } = useSidebarTree();

  // Fetch current space (for permission checks)
  const { isLoading: loadingSpace } = useSpace(spaceId);

  // Redirect to first space if none selected
  const isStandaloneGlobalRoute =
    location.pathname === "/users" ||
    location.pathname === "/spaces-list" ||
    location.pathname === "/audit";

  useEffect(() => {
    if (
      !(loadingTree || spaceId) &&
      spaces.length > 0 &&
      !isStandaloneGlobalRoute
    ) {
      navigate(`/spaces/${spaces[0].id}`, { replace: true });
    }
  }, [loadingTree, spaceId, spaces, isStandaloneGlobalRoute, navigate]);

  // Fetch space permissions
  const { isLoading: permissionsLoading, error: permissionsError } =
    useSpacePermissions(spaceId || "");
  const hasAnyPermission = useHasAnyPermission(spaceId || "");
  const isOrgAdmin = useIsOrgAdmin();

  // Get selectedId from current URL
  const selectedId = getSelectedIdFromPath(
    location.pathname,
    spaceId,
    nodeId,
    rootId
  );

  // Check if the nodeId from URL is valid (only after data is loaded)
  const isValidNodeId = useMemo(() => {
    if (loadingTree) {
      return true; // Don't validate while loading
    }
    if (!nodeId) {
      return true; // Root path is always valid
    }
    if (
      [
        "resources",
        "trash",
        "users",
        "spaces-list",
        "audit-log",
        "settings",
        "permissions",
      ].includes(selectedId)
    ) {
      return true; // Special sections are valid
    }
    return nodes[selectedId] !== undefined; // Check if node exists
  }, [loadingTree, nodeId, selectedId, nodes]);

  // Function to update URL when selection changes
  const setSelectedId = (id: string, targetSpaceId?: string) => {
    const effectiveSpaceId = targetSpaceId || spaceId;
    const newPath = getPathFromSelectedId(id, nodes, effectiveSpaceId);
    navigate(newPath);
  };

  const [endpoints, setEndpoints] = useState<Record<string, Endpoint>>(() => {
    return {};
  });

  // Mutation callbacks that invalidate the cached tree data
  // The actual API calls are made by child components, then they call these to refresh the tree

  const updateNode = (_node: SpaceNode) => {
    // After a node is updated via API, invalidate the tree cache to refetch
    invalidateTree();
  };

  const insertFolderNode = (_folder: FolderNode) => {
    // After a folder is created via API, invalidate the tree cache to refetch
    invalidateTree();
  };

  const addFolder = (_parentId: string) => {
    // This is called after folder creation - invalidate to refresh
    invalidateTree();
  };

  const addApp = async (
    parentId: string,
    name = "New App",
    resourceId = "",
    urlPath = "",
    urlParameters: KeyValuePair[] = [],
    headers: KeyValuePair[] = [],
    body: KeyValuePair[] = [],
    inputs: AppField[] = []
  ) => {
    // Map frontend AppField[] to backend AppInputDto[] format
    const backendInputs = inputs.map((f) => ({
      input: {
        title: f.label,
        type: toBackendInputType(f.type),
      },
      required: f.required ?? false,
    }));

    // Call backend API to create the app
    const response = await createAppAPI({
      name: name.trim(),
      folderId: parentId === "root" ? null : parentId,
      inputs: backendInputs,
      resourceId: resourceId || "",
      urlPath: urlPath || "",
      body: body,
      headers: headers,
      urlParameters: urlParameters,
    });

    if (response.error) {
      throw new Error(
        (response.error.message as string) || "Failed to create app"
      );
    }

    // Use the ID from the API response
    const id = response.data?.id;
    if (!id) {
      throw new Error("Failed to get app ID from response");
    }

    // Invalidate tree cache to refetch with the new app
    invalidateTree();
  };

  const deleteNode = (_id: string) => {
    // After a node is deleted via API, invalidate the tree cache to refetch
    invalidateTree();
  };

  const createEndpoint = () => {
    const id = crypto.randomUUID();
    setEndpoints((prev) => ({
      ...prev,
      [id]: {
        id,
        name: `Endpoint ${Object.keys(prev).length + 1}`,
        url: "https://httpbin.org/post",
        method: "POST",
        headers: [],
        query: [],
        body: [],
      },
    }));
  };
  const updateEndpoint = (ep: Endpoint) =>
    setEndpoints((prev) => ({ ...prev, [ep.id]: ep }));
  const deleteEndpoint = (id: string) =>
    setEndpoints((prev) => {
      const n = { ...prev };
      delete n[id];
      return n;
    });

  const breadcrumbs = useMemo(
    () => buildBreadcrumb(nodes, selectedId, spaces, spaceId),
    [nodes, selectedId, spaces, spaceId]
  );

  // Get current space name for display
  const currentSpace = spaces.find((s) => s.id === spaceId);
  const spaceName = currentSpace?.name || "Space";

  // Show loading state (combined space data + permissions)
  if (loadingTree || permissionsLoading || loadingSpace) {
    return (
      <div className="h-screen flex overflow-hidden">
        <aside className="w-64 border-r bg-card/40 flex items-center justify-center">
          <div className="text-center text-muted-foreground">
            <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-gray-900 mx-auto mb-2" />
            Loading space...
          </div>
        </aside>
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center text-muted-foreground">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900 mx-auto mb-4" />
            Loading...
          </div>
        </div>
      </div>
    );
  }

  // Show permissions error state
  if (permissionsError) {
    return (
      <div className="h-screen flex items-center justify-center">
        <Card className="max-w-md">
          <CardHeader>
            <CardTitle className="text-red-600">Permission Error</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Failed to load your permissions for this space.
            </p>
            <Button onClick={() => window.location.reload()}>Retry</Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Check if we're on a global route that doesn't require spaces
  const isGlobalRoute = ["users", "spaces-list", "audit-log"].includes(
    selectedId
  );

  const hasSpaces = spaces.length > 0;

  // Show empty state when no spaces exist (before checking permissions)
  // But only for space-specific routes, not global routes like /users or /audit
  const showNoSpacesState = !(hasSpaces || isGlobalRoute);

  // Only check for "No Access" when a space exists, we're on a space route,
  // and the user isn't an org admin (org admins should always bypass this state).
  const shouldCheckSpaceAccess = hasSpaces && !!spaceId && !isGlobalRoute;

  const showNoAccessState =
    shouldCheckSpaceAccess &&
    !permissionsLoading &&
    !hasAnyPermission &&
    !isOrgAdmin;
  if (showNoAccessState) {
    return (
      <div className="h-screen flex items-center justify-center">
        <Card className="max-w-md">
          <CardHeader>
            <CardTitle>No Access</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">
              You don't have any permissions on this space.
            </p>
            <p className="text-sm text-muted-foreground">
              To gain access, ask your space moderator to grant you permissions.
            </p>
            <Button onClick={() => navigate("/")}>Back to Home</Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Show error state
  if (treeError) {
    return (
      <div className="h-screen flex overflow-hidden">
        <aside className="w-64 border-r bg-card/40 flex items-center justify-center">
          <div className="text-center text-red-500 p-4">
            <div className="text-2xl mb-2">⚠️</div>
            Error loading space
          </div>
        </aside>
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <div className="text-red-500 text-xl mb-2">⚠️</div>
            <p className="text-red-500 mb-4">
              {treeError instanceof Error
                ? treeError.message
                : "Failed to load space data"}
            </p>
            <button
              type="button"
              onClick={() => window.location.reload()}
              className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Show NotFound for invalid nodeIds
  if (!isValidNodeId) {
    return <NotFound />;
  }

  // Show empty space state (space exists but has no folders/apps)
  // Skip for global routes which don't need space content
  if (Object.keys(nodes).length === 0 && !isGlobalRoute && !showNoSpacesState) {
    return (
      <div className="h-screen flex overflow-hidden">
        <SidebarTree
          nodes={nodes}
          rootId={rootId}
          selectedId={selectedId}
          onSelect={setSelectedId}
          spaceId={spaceId || ""}
          spaces={spaces}
        />
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center text-muted-foreground">
            <div className="text-4xl mb-4">📁</div>
            <h2 className="text-xl font-semibold mb-2">This space is empty</h2>
            <p>Create your first folder or app to get started.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="h-screen flex overflow-hidden">
      <SidebarTree
        nodes={nodes}
        rootId={rootId}
        selectedId={selectedId}
        onSelect={setSelectedId}
        spaceId={spaceId || ""}
        spaces={spaces}
      />
      <div className="flex-1 flex flex-col">
        <header className="flex items-center justify-between px-6 py-4 border-b bg-card/30">
          <h1 className="text-xl font-semibold">Freetool</h1>
          <nav
            className="flex items-center gap-2 text-sm text-muted-foreground"
            aria-label="Breadcrumb"
          >
            {breadcrumbs.map((b, i) => (
              <span key={b.id} className="flex items-center gap-2">
                {i > 0 && <span>/</span>}
                <button
                  type="button"
                  className="hover:text-foreground"
                  onClick={() => setSelectedId(b.id)}
                >
                  {b.name}
                </button>
              </span>
            ))}
          </nav>
        </header>
        {showNoSpacesState ? (
          <div className="flex-1 flex items-center justify-center">
            <Card className="max-w-md">
              <CardHeader>
                <CardTitle>No Spaces Available</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <p className="text-sm text-muted-foreground">
                  {isOrgAdmin
                    ? "Get started by creating a space."
                    : "Contact your administrator to get access to a space."}
                </p>
                {isOrgAdmin ? (
                  <Button onClick={() => setSelectedId("spaces-list")}>
                    Go to Spaces
                  </Button>
                ) : null}
              </CardContent>
            </Card>
          </div>
        ) : (
          <SpaceMain
            nodes={nodes}
            selectedId={selectedId}
            onSelect={setSelectedId}
            updateNode={updateNode}
            insertFolderNode={insertFolderNode}
            createFolder={addFolder}
            createApp={addApp}
            deleteNode={deleteNode}
            endpoints={endpoints}
            createEndpoint={createEndpoint}
            updateEndpoint={updateEndpoint}
            deleteEndpoint={deleteEndpoint}
            spaceId={spaceId || ""}
            spaceName={spaceName}
          />
        )}
      </div>
    </div>
  );
}

// Export wrapped in AuthorizationProvider
export default function Workspace() {
  return (
    <AuthorizationProvider>
      <WorkspaceContent />
    </AuthorizationProvider>
  );
}

function buildBreadcrumb(
  nodes: Record<string, SpaceNode>,
  id: string,
  spaces: SpaceWithDetails[],
  currentSpaceId?: string
) {
  // Handle special sections
  if (id === "resources" && currentSpaceId) {
    const space = spaces.find((s) => s.id === currentSpaceId);
    const spaceName = space?.name || "Space";
    return [
      { id: "root", name: spaceName },
      { id: "resources", name: "Resources" },
    ];
  }
  if (id === "resources") {
    return [{ id: "resources", name: "Resources" }];
  }
  if (id === "trash" && currentSpaceId) {
    const space = spaces.find((s) => s.id === currentSpaceId);
    const spaceName = space?.name || "Space";
    return [
      { id: "root", name: spaceName },
      { id: "trash", name: "Trash" },
    ];
  }
  if (id === "trash") {
    return [{ id: "trash", name: "Trash" }];
  }
  if (id === "users") {
    return [{ id: "users", name: "Users" }];
  }
  if (id === "spaces-list") {
    return [{ id: "spaces-list", name: "Spaces" }];
  }
  if (id === "audit-log") {
    return [{ id: "audit-log", name: "Audit Log" }];
  }
  if ((id === "settings" || id === "permissions") && currentSpaceId) {
    const space = spaces.find((s) => s.id === currentSpaceId);
    const spaceName = space?.name || "Space";
    return [
      { id: "spaces-list", name: "Spaces" },
      { id: "settings", name: `${spaceName} Settings` },
    ];
  }

  const list: { id: string; name: string }[] = [];
  let cur = nodes[id];
  while (cur) {
    list.unshift({ id: cur.id, name: cur.name });
    if (!cur.parentId) {
      break;
    }
    cur = nodes[cur.parentId];
  }
  return list;
}

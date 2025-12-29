import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  createApp as createAppAPI,
  getAppByFolder,
  getFoldersBySpace,
} from "@/api/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { AuthorizationProvider } from "@/contexts/AuthorizationContext";
import {
  useHasAnyPermission,
  useIsOrgAdmin,
  useSpacePermissions,
} from "@/hooks/usePermissions";
import { useSpace, useSpaces } from "@/hooks/useSpaces";
import NotFound from "@/pages/NotFound";
import type { SpaceWithDetails } from "@/types/space";
import SidebarTree from "./SidebarTree";
import SpaceMain from "./SpaceMain";
import type {
  AppNode,
  Endpoint,
  FolderNode,
  KeyValuePair,
  SpaceNode,
} from "./types";

function createFolder(id: string, name: string, parentId?: string): FolderNode {
  return { id, name, type: "folder", parentId, childrenIds: [] };
}
function createApp(id: string, name: string, parentId?: string): AppNode {
  return {
    id,
    name,
    type: "app",
    parentId,
    fields: [],
    endpointId: undefined,
    resourceId: undefined,
  };
}

// API folder response interface
interface ApiFolderResponse {
  id?: string;
  name: string;
  parentId?: string | null;
  children?: ApiFolderResponse[] | null;
}

// Transform API folder data to space nodes structure
function transformFoldersToNodes(
  folders: ApiFolderResponse[],
  rootId: string,
  spaceId?: string
): Record<string, SpaceNode> {
  const nodes: Record<string, SpaceNode> = {};

  // Create virtual root node
  const root: FolderNode = {
    id: rootId,
    name: "Spaces",
    type: "folder",
    childrenIds: [],
    spaceId,
  };
  nodes[rootId] = root;

  // First pass: create all folder nodes
  folders.forEach((folder) => {
    if (folder.id) {
      const folderNode: FolderNode = {
        id: folder.id,
        name: folder.name || "Unnamed Folder",
        type: "folder",
        parentId: folder.parentId ?? rootId, // Use nullish coalescing to handle null parentId
        childrenIds: [],
        spaceId,
      };
      nodes[folder.id] = folderNode;
    }
  });

  // Second pass: build parent-child relationships
  Object.values(nodes).forEach((node) => {
    if (node.type === "folder" && node.parentId && nodes[node.parentId]) {
      const parent = nodes[node.parentId] as FolderNode;
      if (!parent.childrenIds.includes(node.id)) {
        parent.childrenIds.push(node.id);
      }
    }
  });

  return nodes;
}

// Helper functions to convert between selectedId and URL paths
function getPathFromSelectedId(
  selectedId: string,
  nodes: Record<string, SpaceNode>,
  spaceId?: string
): string {
  if (selectedId === "users-&-spaces") {
    return "/users";
  }
  if (selectedId === "audit-log") {
    return "/audit";
  }
  if (selectedId === "resources") {
    // Resources are now space-specific
    return spaceId ? `/spaces/${spaceId}/resources` : "/spaces";
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
    return "users-&-spaces";
  }
  if (pathname === "/audit") {
    return "audit-log";
  }
  if (pathname === `/spaces/${spaceId}/resources`) {
    return "resources";
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

  // Fetch all spaces
  const { spaces, isLoading: loadingSpaces } = useSpaces();

  // Fetch current space
  const { isLoading: loadingSpace } = useSpace(spaceId);

  // Redirect to first space if none selected
  useEffect(() => {
    const isStandaloneGlobalRoute =
      location.pathname === "/users" || location.pathname === "/audit";

    if (
      !(loadingSpaces || spaceId) &&
      spaces.length > 0 &&
      !isStandaloneGlobalRoute
    ) {
      navigate(`/spaces/${spaces[0].id}`, { replace: true });
    }
  }, [spaceId, spaces, loadingSpaces, navigate, location.pathname]);

  // Fetch space permissions
  const { isLoading: permissionsLoading, error: permissionsError } =
    useSpacePermissions(spaceId || "");
  const hasAnyPermission = useHasAnyPermission(spaceId || "");
  const isOrgAdmin = useIsOrgAdmin();

  const [nodes, setNodes] = useState<Record<string, SpaceNode>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [loadedFolders, setLoadedFolders] = useState<Set<string>>(new Set());
  const rootId = "root";

  // Load space data
  useEffect(() => {
    const loadSpaceData = async () => {
      if (!spaceId) {
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        const response = await getFoldersBySpace(spaceId);

        if (response.error) {
          setError("Failed to load space data");
        } else if (response.data) {
          // Transform API response to nodes structure
          const transformedNodes = transformFoldersToNodes(
            response.data.items || [],
            rootId,
            spaceId
          );
          setNodes(transformedNodes);
        }
      } catch (_err) {
        setError("Failed to load space data");
      } finally {
        setLoading(false);
      }
    };

    loadSpaceData();
  }, [spaceId]);

  // Get selectedId from current URL
  const selectedId = getSelectedIdFromPath(
    location.pathname,
    spaceId,
    nodeId,
    rootId
  );

  // Load apps when navigating to a folder
  useEffect(() => {
    const loadAppsForFolder = async () => {
      // Only load apps if we have a valid folder selected, nodes are loaded, and we haven't loaded this folder yet
      // Skip loading apps for root folder since apps cannot live in the root
      if (
        loading ||
        !nodes[selectedId] ||
        nodes[selectedId].type !== "folder" ||
        loadedFolders.has(selectedId) ||
        selectedId === rootId
      ) {
        return;
      }

      try {
        const response = await getAppByFolder(selectedId);

        if (!response.error && response.data?.items) {
          // Transform API apps to AppNode format and add them to nodes
          const appsToAdd: Record<string, SpaceNode> = {};
          const appIds: string[] = [];

          response.data.items.forEach((apiApp) => {
            if (apiApp.id) {
              const appNode: AppNode = {
                id: apiApp.id,
                name: apiApp.name || "Unnamed App",
                type: "app",
                parentId: selectedId,
                fields: [], // TODO: Transform apiApp inputs to fields if needed
                endpointId: undefined, // TODO: Map from apiApp if available
                resourceId: apiApp.resourceId || undefined,
                urlPath: apiApp.urlPath || "",
                urlParameters: (apiApp.urlParameters || []).map((kvp) => ({
                  key: kvp.key || "",
                  value: kvp.value || "",
                })),
                headers: (apiApp.headers || []).map((kvp) => ({
                  key: kvp.key || "",
                  value: kvp.value || "",
                })),
                body: (apiApp.body || []).map((kvp) => ({
                  key: kvp.key || "",
                  value: kvp.value || "",
                })),
              };
              appsToAdd[apiApp.id] = appNode;
              appIds.push(apiApp.id);
            }
          });

          // Update nodes state with new apps and update folder's childrenIds
          setNodes((prev) => {
            const folder = prev[selectedId] as FolderNode;
            // Merge existing childrenIds with new app IDs, avoiding duplicates
            const existingChildIds = folder.childrenIds || [];
            const newChildIds = [...new Set([...existingChildIds, ...appIds])];

            return {
              ...prev,
              ...appsToAdd,
              [selectedId]: {
                ...folder,
                childrenIds: newChildIds,
              },
            };
          });

          // Mark this folder as loaded
          setLoadedFolders((prev) => new Set([...prev, selectedId]));
        }
      } catch (_error) {
        // Silently ignore folder loading errors
      }
    };

    loadAppsForFolder();
  }, [selectedId, loading, loadedFolders, nodes]);

  // Check if the nodeId from URL is valid (only after data is loaded)
  const isValidNodeId = useMemo(() => {
    if (loading) {
      return true; // Don't validate while loading
    }
    if (!nodeId) {
      return true; // Root path is always valid
    }
    if (["resources", "users-&-teams", "audit-log"].includes(selectedId)) {
      return true; // Special sections are valid
    }
    return nodes[selectedId] !== undefined; // Check if node exists
  }, [loading, nodeId, selectedId, nodes]);

  // Function to update URL when selection changes
  const setSelectedId = (id: string, targetSpaceId?: string) => {
    const effectiveSpaceId = targetSpaceId || spaceId;
    const newPath = getPathFromSelectedId(id, nodes, effectiveSpaceId);
    navigate(newPath);
  };

  const [endpoints, setEndpoints] = useState<Record<string, Endpoint>>(() => {
    return {};
  });

  const updateNode = (node: SpaceNode) =>
    setNodes((prev) => ({ ...prev, [node.id]: node }));

  const insertFolderNode = (folder: FolderNode) =>
    setNodes((prev) => {
      const parentId = folder.parentId || rootId;
      const parentNode = prev[parentId];

      if (!parentNode || parentNode.type !== "folder") {
        return {
          ...prev,
          [folder.id]: folder,
        };
      }

      const hasChild = parentNode.childrenIds.includes(folder.id);
      const updatedParent: FolderNode = {
        ...parentNode,
        childrenIds: hasChild
          ? parentNode.childrenIds
          : [...parentNode.childrenIds, folder.id],
      };

      return {
        ...prev,
        [folder.id]: folder,
        [parentId]: updatedParent,
      };
    });

  const addFolder = (parentId: string) =>
    setNodes((prev) => {
      const id = crypto.randomUUID();
      const folder = createFolder(id, "New Folder", parentId);
      const parent = prev[parentId] as FolderNode;
      return {
        ...prev,
        [id]: folder,
        [parentId]: { ...parent, childrenIds: [...parent.childrenIds, id] },
      };
    });

  const addApp = async (
    parentId: string,
    name = "New App",
    resourceId = "",
    urlPath = "",
    urlParameters: KeyValuePair[] = [],
    headers: KeyValuePair[] = [],
    body: KeyValuePair[] = []
  ) => {
    // Call backend API to create the app
    const response = await createAppAPI({
      name: name.trim(),
      folderId: parentId === "root" ? null : parentId,
      inputs: [],
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

    // Update local state only if API call succeeds
    setNodes((prev) => {
      const app = createApp(id, name.trim(), parentId);
      const parent = prev[parentId] as FolderNode;
      return {
        ...prev,
        [id]: app,
        [parentId]: { ...parent, childrenIds: [...parent.childrenIds, id] },
      };
    });

    // Mark the parent folder as loaded (since we're adding an app to it)
    setLoadedFolders((prev) => new Set([...prev, parentId]));
  };

  const deleteNode = (id: string) =>
    setNodes((prev) => {
      const toDelete = collectSubtreeIds(prev, id);
      const newNodes: Record<string, SpaceNode> = {};
      for (const key in prev) {
        if (!toDelete.has(key)) {
          newNodes[key] = prev[key];
        }
      }
      // remove from parent's children
      const parentId = prev[id]?.parentId;
      if (parentId && newNodes[parentId]?.type === "folder") {
        const parent = newNodes[parentId] as FolderNode;
        newNodes[parentId] = {
          ...parent,
          childrenIds: parent.childrenIds.filter((cid) => cid !== id),
        };
      }
      return newNodes;
    });

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
  if (loading || permissionsLoading || loadingSpaces || loadingSpace) {
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
  const isGlobalRoute = ["users-&-spaces", "audit-log"].includes(selectedId);

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
  if (error) {
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
            <p className="text-red-500 mb-4">{error}</p>
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
                  <Button onClick={() => setSelectedId("users-&-spaces")}>
                    Go to Users & Spaces
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
  if (id === "users-&-spaces") {
    return [{ id: "users-&-spaces", name: "Users & Spaces" }];
  }
  if (id === "audit-log") {
    return [{ id: "audit-log", name: "Audit Log" }];
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

function collectSubtreeIds(nodes: Record<string, SpaceNode>, id: string) {
  const set = new Set<string>();
  const walk = (nid: string) => {
    set.add(nid);
    const n = nodes[nid];
    if (n?.type === "folder") {
      n.childrenIds.forEach(walk);
    }
  };
  walk(id);
  return set;
}

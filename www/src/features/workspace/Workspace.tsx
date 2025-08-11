import { useMemo, useState, useEffect } from "react";
import { useParams, useLocation, useNavigate } from "react-router-dom";
import SidebarTree from "./SidebarTree";
import WorkspaceMain from "./WorkspaceMain";
import { WorkspaceNode, FolderNode, AppNode, Endpoint } from "./types";
import { getAllFolders } from "@/api/api";

function createFolder(id: string, name: string, parentId?: string): FolderNode {
  return { id, name, type: "folder", parentId, childrenIds: [] };
}
function createApp(id: string, name: string, parentId?: string): AppNode {
  return { id, name, type: "app", parentId, fields: [], endpointId: undefined };
}

// Transform API folder data to workspace nodes structure
function transformFoldersToNodes(
  folders: Array<{
    id?: string;
    name: string;
    parentId?: string | null;
    children?: any[] | null;
  }>,
  rootId: string,
): Record<string, WorkspaceNode> {
  const nodes: Record<string, WorkspaceNode> = {};

  // Create virtual root node
  const root: FolderNode = {
    id: rootId,
    name: "Workspaces",
    type: "folder",
    childrenIds: [],
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
  nodes: Record<string, WorkspaceNode>,
): string {
  if (selectedId === "resources") return "/resources";
  if (selectedId === "users-&-teams") return "/users";
  if (selectedId === "audit-log") return "/audit";
  if (selectedId === "root") return "/workspaces";

  // For workspace nodes, use /workspaces/:nodeId
  if (nodes[selectedId]) {
    return `/workspaces/${selectedId}`;
  }

  return "/workspaces";
}

function getSelectedIdFromPath(
  pathname: string,
  nodeId?: string,
  rootId: string = "root",
): string {
  if (pathname === "/resources") return "resources";
  if (pathname === "/users") return "users-&-teams";
  if (pathname === "/audit") return "audit-log";
  if (pathname === "/workspaces" && !nodeId) return rootId;
  if (pathname.startsWith("/workspaces/") && nodeId) return nodeId;
  if (pathname === "/") return rootId;

  return rootId;
}

export default function Workspace() {
  const { nodeId } = useParams<{ nodeId?: string }>();
  const location = useLocation();
  const navigate = useNavigate();

  const [nodes, setNodes] = useState<Record<string, WorkspaceNode>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const rootId = "root";

  // Load workspace data
  useEffect(() => {
    const loadWorkspaceData = async () => {
      try {
        setLoading(true);
        setError(null);
        const response = await getAllFolders();

        if (response.error) {
          setError("Failed to load workspace data");
          console.error("Error loading workspace data:", response.error);
        } else if (response.data) {
          // Transform API response to nodes structure
          const transformedNodes = transformFoldersToNodes(
            response.data.items || [],
            rootId,
          );
          setNodes(transformedNodes);
        }
      } catch (err) {
        setError("Failed to load workspace data");
        console.error("Error loading workspace data:", err);
      } finally {
        setLoading(false);
      }
    };

    loadWorkspaceData();
  }, [rootId]);

  // Get selectedId from current URL
  const selectedId = getSelectedIdFromPath(location.pathname, nodeId, rootId);

  // Function to update URL when selection changes
  const setSelectedId = (id: string) => {
    const newPath = getPathFromSelectedId(id, nodes);
    navigate(newPath);
  };

  const [endpoints, setEndpoints] = useState<Record<string, Endpoint>>(() => {
    return {};
  });

  const updateNode = (node: WorkspaceNode) =>
    setNodes((prev) => ({ ...prev, [node.id]: node }));

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

  const addApp = (parentId: string) =>
    setNodes((prev) => {
      const id = crypto.randomUUID();
      const app = createApp(id, "New App", parentId);
      const parent = prev[parentId] as FolderNode;
      return {
        ...prev,
        [id]: app,
        [parentId]: { ...parent, childrenIds: [...parent.childrenIds, id] },
      };
    });

  const deleteNode = (id: string) =>
    setNodes((prev) => {
      const toDelete = collectSubtreeIds(prev, id);
      const newNodes: Record<string, WorkspaceNode> = {};
      for (const key in prev) {
        if (!toDelete.has(key)) newNodes[key] = prev[key];
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
    () => buildBreadcrumb(nodes, selectedId),
    [nodes, selectedId],
  );

  // Show loading state
  if (loading) {
    return (
      <div className="h-screen flex overflow-hidden">
        <aside className="w-64 border-r bg-card/40 flex items-center justify-center">
          <div className="text-center text-muted-foreground">
            <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-gray-900 mx-auto mb-2"></div>
            Loading workspace...
          </div>
        </aside>
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center text-muted-foreground">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900 mx-auto mb-4"></div>
            Loading...
          </div>
        </div>
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
            Error loading workspace
          </div>
        </aside>
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <div className="text-red-500 text-xl mb-2">⚠️</div>
            <p className="text-red-500 mb-4">{error}</p>
            <button
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

  // Show empty state
  if (Object.keys(nodes).length === 0) {
    return (
      <div className="h-screen flex overflow-hidden">
        <aside className="w-64 border-r bg-card/40 flex items-center justify-center">
          <div className="text-center text-muted-foreground p-4">
            <div className="text-2xl mb-2">📁</div>
            No workspaces
          </div>
        </aside>
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center text-muted-foreground">
            <div className="text-4xl mb-4">📁</div>
            <h2 className="text-xl font-semibold mb-2">No workspaces found</h2>
            <p>Create your first workspace to get started.</p>
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
                  className="hover:text-foreground"
                  onClick={() => setSelectedId(b.id)}
                >
                  {b.name}
                </button>
              </span>
            ))}
          </nav>
        </header>
        <WorkspaceMain
          nodes={nodes}
          selectedId={selectedId}
          onSelect={setSelectedId}
          updateNode={updateNode}
          createFolder={addFolder}
          createApp={addApp}
          deleteNode={deleteNode}
          endpoints={endpoints}
          createEndpoint={createEndpoint}
          updateEndpoint={updateEndpoint}
          deleteEndpoint={deleteEndpoint}
        />
      </div>
    </div>
  );
}

function buildBreadcrumb(nodes: Record<string, WorkspaceNode>, id: string) {
  // Handle special sections
  if (id === "resources") {
    return [{ id: "resources", name: "Resources" }];
  }
  if (id === "users-&-teams") {
    return [{ id: "users-&-teams", name: "Users & Teams" }];
  }
  if (id === "audit-log") {
    return [{ id: "audit-log", name: "Audit Log" }];
  }

  const list: { id: string; name: string }[] = [];
  let cur = nodes[id];
  while (cur) {
    list.unshift({ id: cur.id, name: cur.name });
    if (!cur.parentId) break;
    cur = nodes[cur.parentId];
  }
  return list;
}

function collectSubtreeIds(nodes: Record<string, WorkspaceNode>, id: string) {
  const set = new Set<string>();
  const walk = (nid: string) => {
    set.add(nid);
    const n = nodes[nid];
    if (n?.type === "folder") n.childrenIds.forEach(walk);
  };
  walk(id);
  return set;
}

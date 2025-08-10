import { useMemo, useState } from "react";
import SidebarTree from "./SidebarTree";
import WorkspaceMain from "./WorkspaceMain";
import { WorkspaceNode, FolderNode, AppNode, Endpoint } from "./types";

function createFolder(id: string, name: string, parentId?: string): FolderNode {
  return { id, name, type: "folder", parentId, childrenIds: [] };
}
function createApp(id: string, name: string, parentId?: string): AppNode {
  return { id, name, type: "app", parentId, fields: [], endpointId: undefined };
}

export default function Workspace() {
  const [nodes, setNodes] = useState<Record<string, WorkspaceNode>>(() => {
    const rootId = "root";
    const folder1 = createFolder(crypto.randomUUID(), "Team A", rootId);
    const app1 = createApp(crypto.randomUUID(), "Onboarding App", folder1.id);
    const root: FolderNode = createFolder(rootId, "Workspaces");
    root.childrenIds = [folder1.id];
    folder1.childrenIds = [app1.id];
    return { [root.id]: root, [folder1.id]: folder1, [app1.id]: app1 };
  });
  const rootId = "root";
  const [selectedId, setSelectedId] = useState<string>(rootId);

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

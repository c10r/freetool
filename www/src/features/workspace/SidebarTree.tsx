import {
  ChevronDown,
  ChevronRight,
  Database,
  FileType,
  FolderClosed,
  FolderOpen,
  Shield,
  Users,
} from "lucide-react";
import { useMemo, useState } from "react";
import { cn } from "@/lib/utils";
import type { WorkspaceWithGroup } from "@/types/workspace";
import type { TreeNode, WorkspaceNode } from "./types";

interface SidebarTreeProps {
  nodes: Record<string, WorkspaceNode>;
  rootId: string;
  selectedId: string;
  onSelect: (id: string, targetWorkspaceId?: string) => void;
  workspaceId: string;
  workspaces: WorkspaceWithGroup[];
}

export default function SidebarTree({
  nodes,
  rootId,
  selectedId,
  onSelect,
  workspaceId,
  workspaces,
}: SidebarTreeProps) {
  const [expandedWorkspaces, setExpandedWorkspaces] = useState<Set<string>>(
    () => new Set([workspaceId])
  );
  const [expandedFolders, setExpandedFolders] = useState<Set<string>>(
    new Set()
  );

  const toggleWorkspace = (wsId: string) => {
    setExpandedWorkspaces((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(wsId)) {
        newSet.delete(wsId);
      } else {
        newSet.add(wsId);
      }
      return newSet;
    });
  };

  const toggleFolder = (folderId: string) => {
    setExpandedFolders((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(folderId)) {
        newSet.delete(folderId);
      } else {
        newSet.add(folderId);
      }
      return newSet;
    });
  };

  // Group folders by workspace
  const foldersByWorkspace = useMemo(() => {
    const grouped: Record<string, WorkspaceNode[]> = {};
    Object.values(nodes).forEach((node) => {
      if (node.type === "folder") {
        const wsId = node.workspaceId || "unknown";
        if (!grouped[wsId]) {
          grouped[wsId] = [];
        }
        // Only include top-level workspace folders (those whose parent is the virtual root)
        // This excludes the virtual root node itself (which has no parentId)
        if (node.parentId === rootId) {
          grouped[wsId].push(node);
        }
      }
    });
    return grouped;
  }, [nodes, rootId]);

  return (
    <aside className="w-64 border-r bg-card/40">
      <nav className="p-3 space-y-2">
        {/* Workspaces header - always show */}
        <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wider px-2 py-1">
          Workspaces
        </div>

        {/* Workspace sections */}
        {workspaces.length === 0 ? (
          <button
            type="button"
            onClick={() => onSelect("root")}
            className={cn(
              "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left hover:bg-accent font-semibold",
              selectedId === "root" && "bg-accent"
            )}
          >
            <FolderOpen size={16} />
            <span>Workspaces</span>
          </button>
        ) : (
          workspaces.map((workspace) => {
            const isExpanded = expandedWorkspaces.has(workspace.id);
            const isCurrentWorkspace = workspace.id === workspaceId;
            const rootFolders = foldersByWorkspace[workspace.id] || [];
            const workspaceLabel = workspace.groupName || workspace.id;

            return (
              <div key={workspace.id}>
                <div
                  className={cn(
                    "flex items-center rounded-md hover:bg-accent",
                    isCurrentWorkspace && "bg-accent/50"
                  )}
                >
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleWorkspace(workspace.id);
                    }}
                    className="flex-shrink-0 p-1.5 hover:bg-accent/50 rounded"
                    aria-label={
                      isExpanded ? "Collapse workspace" : "Expand workspace"
                    }
                  >
                    {isExpanded ? (
                      <ChevronDown size={16} />
                    ) : (
                      <ChevronRight size={16} />
                    )}
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      // Auto-expand when navigating to workspace
                      if (!expandedWorkspaces.has(workspace.id)) {
                        toggleWorkspace(workspace.id);
                      }
                      onSelect("root", workspace.id);
                    }}
                    className="flex items-center gap-2 px-2 py-1.5 text-left font-semibold flex-1"
                  >
                    {isExpanded ? (
                      <FolderOpen size={16} />
                    ) : (
                      <FolderClosed size={16} />
                    )}
                    <span>{workspaceLabel}</span>
                  </button>
                </div>

                {isExpanded && (
                  <div className="ml-4 mt-1 space-y-1">
                    {/* Resources (workspace-specific) */}
                    <button
                      type="button"
                      onClick={() => onSelect("resources", workspace.id)}
                      className={cn(
                        "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left hover:bg-accent",
                        selectedId === "resources" &&
                          isCurrentWorkspace &&
                          "bg-accent"
                      )}
                    >
                      <Database size={16} />
                      <span>Resources</span>
                    </button>

                    {/* Workspace folders and apps */}
                    {rootFolders.map((folder) => {
                      const node = buildTree(nodes, folder.id);
                      if (!node) {
                        return null;
                      }
                      return (
                        <TreeNodeComponent
                          key={folder.id}
                          node={node}
                          depth={1}
                          selectedId={selectedId}
                          onSelect={onSelect}
                          expandedFolders={expandedFolders}
                          toggleExpanded={toggleFolder}
                        />
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })
        )}

        {/* Global sections separator */}
        <div className="border-t my-4 pt-4" />

        {/* Users & Teams (global) */}
        <div>
          <button
            type="button"
            onClick={() => onSelect("users-&-teams")}
            className={cn(
              "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left hover:bg-accent font-semibold",
              selectedId === "users-&-teams" && "bg-accent"
            )}
          >
            <Users size={16} />
            <span>Users & Teams</span>
          </button>
        </div>

        {/* Audit Log (global) */}
        <div>
          <button
            type="button"
            onClick={() => onSelect("audit-log")}
            className={cn(
              "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left hover:bg-accent font-semibold",
              selectedId === "audit-log" && "bg-accent"
            )}
          >
            <Shield size={16} />
            <span>Audit Log</span>
          </button>
        </div>
      </nav>
    </aside>
  );
}

function TreeNodeComponent({
  node,
  depth,
  selectedId,
  onSelect,
  expandedFolders,
  toggleExpanded,
}: {
  node: TreeNode;
  depth: number;
  selectedId: string;
  onSelect: (id: string) => void;
  expandedFolders: Set<string>;
  toggleExpanded: (folderId: string) => void;
}) {
  const isFolder = node.type === "folder";
  const isSelected = node.id === selectedId;
  const isExpanded = expandedFolders.has(node.id);
  const hasChildren = isFolder && node.children && node.children.length > 0;

  const handleChevronClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    toggleExpanded(node.id);
  };

  const handleFolderClick = () => {
    onSelect(node.id);
  };

  return (
    <div>
      <button
        type="button"
        onClick={handleFolderClick}
        className={cn(
          "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left hover:bg-accent",
          isSelected && "bg-accent"
        )}
        aria-current={isSelected ? "page" : undefined}
      >
        {hasChildren && (
          <button
            type="button"
            className="flex-shrink-0 cursor-pointer hover:bg-accent/50 rounded p-0.5"
            onClick={handleChevronClick}
            aria-label={isExpanded ? "Collapse folder" : "Expand folder"}
          >
            {isExpanded ? (
              <ChevronDown size={16} />
            ) : (
              <ChevronRight size={16} />
            )}
          </button>
        )}
        {isFolder ? (
          isExpanded ? (
            <FolderOpen size={16} />
          ) : (
            <FolderClosed size={16} />
          )
        ) : (
          <FileType size={16} />
        )}
        <span className={cn(depth === 0 ? "font-semibold" : "")}>
          {node.name}
        </span>
      </button>
      {hasChildren && isExpanded && (
        <div className="ml-4 mt-1 space-y-1">
          {node.children.map((child: TreeNode) => (
            <TreeNodeComponent
              key={child.id}
              node={child}
              depth={depth + 1}
              selectedId={selectedId}
              onSelect={onSelect}
              expandedFolders={expandedFolders}
              toggleExpanded={toggleExpanded}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function buildTree(
  nodes: Record<string, WorkspaceNode>,
  id: string
): TreeNode | null {
  const n = nodes[id];
  if (!n) {
    return null;
  }
  if (n.type === "folder") {
    return {
      ...n,
      children: n.childrenIds
        .map((cid) => buildTree(nodes, cid))
        .filter((node): node is TreeNode => node !== null),
    };
  }
  return n;
}

import { useMemo, useState } from "react";
import {
  Database,
  FolderClosed,
  FolderOpen,
  FileType,
  Users,
  Shield,
  ChevronRight,
  ChevronDown,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { WorkspaceNode, TreeNode } from "./types";

interface SidebarTreeProps {
  nodes: Record<string, WorkspaceNode>;
  rootId: string;
  selectedId: string;
  onSelect: (id: string) => void;
  workspaceId: string;
}

export default function SidebarTree({
  nodes,
  rootId,
  selectedId,
  onSelect,
  workspaceId,
}: SidebarTreeProps) {
  const tree = useMemo(() => buildTree(nodes, rootId), [nodes, rootId]);
  const [expandedFolders, setExpandedFolders] = useState<Set<string>>(
    new Set([rootId]),
  );

  const toggleExpanded = (folderId: string) => {
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

  return (
    <aside className="w-64 border-r bg-card/40">
      <nav className="p-3 space-y-4">
        <div>
          {tree && (
            <TreeNodeComponent
              node={tree}
              depth={0}
              selectedId={selectedId}
              onSelect={onSelect}
              expandedFolders={expandedFolders}
              toggleExpanded={toggleExpanded}
            />
          )}
        </div>

        <div>
          <SidebarSection
            title="Resources"
            icon={<Database size={16} />}
            selectedId={selectedId}
            onSelect={onSelect}
          />
        </div>

        <div>
          <SidebarSection
            title="Users & Teams"
            icon={<Users size={16} />}
            selectedId={selectedId}
            onSelect={onSelect}
          />
        </div>

        <div>
          <SidebarSection
            title="Audit Log"
            icon={<Shield size={16} />}
            selectedId={selectedId}
            onSelect={onSelect}
          />
        </div>
      </nav>
    </aside>
  );
}

function SidebarSection({
  title,
  icon,
  selectedId,
  onSelect,
}: {
  title: string;
  icon: React.ReactNode;
  selectedId: string;
  onSelect: (id: string) => void;
}) {
  const sectionId = title.toLowerCase().replace(/ /g, "-");
  const isSelected = selectedId === sectionId;

  return (
    <button
      onClick={() => onSelect(sectionId)}
      className={cn(
        "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left hover:bg-accent font-semibold",
        isSelected && "bg-accent",
      )}
      aria-current={isSelected ? "page" : undefined}
    >
      {icon}
      <span>{title}</span>
    </button>
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
        onClick={handleFolderClick}
        className={cn(
          "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left hover:bg-accent",
          isSelected && "bg-accent",
        )}
        aria-current={isSelected ? "page" : undefined}
      >
        {hasChildren && (
          <span
            className="flex-shrink-0 cursor-pointer hover:bg-accent/50 rounded p-0.5"
            onClick={handleChevronClick}
          >
            {isExpanded ? (
              <ChevronDown size={16} />
            ) : (
              <ChevronRight size={16} />
            )}
          </span>
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

function buildTree(nodes: Record<string, WorkspaceNode>, id: string): TreeNode | null {
  const n = nodes[id];
  if (!n) return null;
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

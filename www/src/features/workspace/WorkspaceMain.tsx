import AppView from "./components/AppView";
import AuditLogView from "./components/AuditLogView";
import FolderView from "./components/FolderView";
import ResourcesView from "./components/ResourcesView";
import UsersTeamsView from "./components/UsersTeamsView";
import type { WorkspaceMainProps } from "./types";

export default function WorkspaceMain(props: WorkspaceMainProps) {
  const { nodes, selectedId, workspaceId, workspaceName, onSelect } = props;
  const selected = nodes[selectedId];

  // Handle special sections from sidebar
  if (selectedId === "resources") {
    return (
      <ResourcesView
        workspaceId={workspaceId}
        workspaceName={workspaceName}
        onBackClick={() => onSelect("root")}
      />
    );
  }

  if (selectedId === "audit-log") {
    return <AuditLogView />;
  }

  if (selectedId === "users-&-teams") {
    return <UsersTeamsView />;
  }

  if (!selected) {
    return null;
  }

  if (selected.type === "folder") {
    return <FolderView {...props} folder={selected} />;
  }

  return <AppView {...props} app={selected} />;
}

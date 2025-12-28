import { WorkspaceMainProps } from "./types";
import AppView from "./components/AppView";
import FolderView from "./components/FolderView";
import ResourcesView from "./components/ResourcesView";
import AuditLogView from "./components/AuditLogView";
import UsersTeamsView from "./components/UsersTeamsView";

export default function WorkspaceMain(props: WorkspaceMainProps) {
  const { nodes, selectedId, workspaceId } = props;
  const selected = nodes[selectedId];

  // Handle special sections from sidebar
  if (selectedId === "resources") {
    return <ResourcesView workspaceId={workspaceId} />;
  }

  if (selectedId === "audit-log") {
    return <AuditLogView />;
  }

  if (selectedId === "users-&-teams") {
    return <UsersTeamsView />;
  }

  if (!selected) return null;

  if (selected.type === "folder") {
    return <FolderView {...props} folder={selected} />;
  }

  return <AppView {...props} app={selected} />;
}

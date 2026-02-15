import AppView from "./components/AppView";
import AuditLogView from "./components/AuditLogView";
import DashboardView from "./components/DashboardView";
import FolderView from "./components/FolderView";
import ResourcesView from "./components/ResourcesView";
import SpaceSettingsView from "./components/SpaceSettingsView";
import SpacesView from "./components/SpacesView";
import TrashView from "./components/TrashView";
import UsersView from "./components/UsersView";
import type { SpaceMainProps } from "./types";

export default function WorkspaceMain(props: SpaceMainProps) {
  const { nodes, selectedId, spaceId, spaceName, onSelect } = props;
  const selected = nodes[selectedId];

  // Handle special sections from sidebar
  if (selectedId === "resources") {
    return (
      <ResourcesView
        spaceId={spaceId}
        spaceName={spaceName}
        onBackClick={() => onSelect("root")}
      />
    );
  }

  if (selectedId === "trash") {
    return <TrashView spaceId={spaceId} spaceName={spaceName} />;
  }

  if (selectedId === "audit-log") {
    return <AuditLogView />;
  }

  if (selectedId === "users") {
    return <UsersView />;
  }

  if (selectedId === "spaces-list") {
    return <SpacesView />;
  }

  if (selectedId === "settings") {
    return <SpaceSettingsView onBackClick={() => onSelect("spaces-list")} />;
  }

  // Backwards compatibility: redirect old permissions route to settings
  if (selectedId === "permissions") {
    return <SpaceSettingsView onBackClick={() => onSelect("spaces-list")} />;
  }

  if (!selected) {
    return null;
  }

  if (selected.type === "folder") {
    return <FolderView {...props} folder={selected} />;
  }

  if (selected.type === "dashboard") {
    return <DashboardView {...props} dashboard={selected} />;
  }

  return <AppView {...props} app={selected} />;
}

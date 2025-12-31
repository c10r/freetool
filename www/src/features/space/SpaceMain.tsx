import AppView from "./components/AppView";
import AuditLogView from "./components/AuditLogView";
import FolderView from "./components/FolderView";
import ResourcesView from "./components/ResourcesView";
import SpacePermissionsView from "./components/SpacePermissionsView";
import TrashView from "./components/TrashView";
import UsersSpacesView from "./components/UsersSpacesView";
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

  if (selectedId === "users-&-spaces") {
    return <UsersSpacesView />;
  }

  if (selectedId === "permissions") {
    return <SpacePermissionsView onBackClick={() => onSelect("root")} />;
  }

  if (!selected) {
    return null;
  }

  if (selected.type === "folder") {
    return <FolderView {...props} folder={selected} />;
  }

  return <AppView {...props} app={selected} />;
}

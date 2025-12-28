/**
 * Workspace entity types
 *
 * Workspaces represent top-level organizational containers that have a 1:1
 * relationship with teams. Each workspace contains folders, apps, and resources.
 */

export interface Workspace {
  id: string;
  groupId: string;
  createdAt: string;
  updatedAt: string;
}

export interface WorkspaceWithGroup extends Workspace {
  groupName: string; // For display (fetched from associated group)
}

export interface WorkspaceListResponse {
  items: Workspace[];
  total: number;
  skip: number;
  take: number;
}

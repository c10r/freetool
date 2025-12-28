/**
 * Permission types for the authorization system
 *
 * These types define the fine-grained permissions that can be granted to users
 * on a per-workspace basis via OpenFGA.
 */

/**
 * Individual permission types that can be granted to users
 */
export type Permission =
  | "create_resource"
  | "edit_resource"
  | "delete_resource"
  | "create_app"
  | "edit_app"
  | "delete_app"
  | "run_app"
  | "create_folder"
  | "edit_folder"
  | "delete_folder";

/**
 * Map of all workspace permissions with their granted/denied status
 */
export interface WorkspacePermissions {
  create_resource: boolean;
  edit_resource: boolean;
  delete_resource: boolean;
  create_app: boolean;
  edit_app: boolean;
  delete_app: boolean;
  run_app: boolean;
  create_folder: boolean;
  edit_folder: boolean;
  delete_folder: boolean;
}

/**
 * Team membership information for a user
 */
export interface TeamMembership {
  id: string;
  name: string;
  role: "admin" | "member";
}

/**
 * Current authenticated user information including role data
 */
export interface CurrentUser {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
  isOrgAdmin: boolean;
  teams: TeamMembership[];
}

/**
 * Response from the workspace permissions endpoint
 * Note: This endpoint is not yet implemented in the backend
 */
export interface WorkspacePermissionsResponse {
  workspaceId: string;
  userId: string;
  permissions: WorkspacePermissions;
  isOrgAdmin: boolean;
  isTeamAdmin: boolean;
  teamId?: string;
}

/**
 * Response from the current user endpoint
 * Note: This endpoint is not yet implemented in the backend
 */
export interface CurrentUserResponse {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
  isOrgAdmin: boolean;
  teams: TeamMembership[];
}

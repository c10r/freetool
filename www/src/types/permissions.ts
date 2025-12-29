/**
 * Permission types for the authorization system
 *
 * These types define the fine-grained permissions that can be granted to users
 * on a per-space basis via OpenFGA.
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
 * Map of all space permissions with their granted/denied status
 */
export interface SpacePermissions {
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
 * Space membership information for a user
 */
export interface SpaceMembership {
  id: string;
  name: string;
  role: "moderator" | "member";
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
  spaces: SpaceMembership[];
}

/**
 * Response from the space permissions endpoint
 */
export interface SpacePermissionsResponse {
  spaceId: string;
  userId: string;
  permissions: SpacePermissions;
  isOrgAdmin: boolean;
  isSpaceModerator: boolean;
}

/**
 * Response from the current user endpoint
 */
export interface CurrentUserResponse {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
  isOrgAdmin: boolean;
  spaces: SpaceMembership[];
}

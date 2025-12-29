/**
 * Space entity types
 *
 * Spaces are unified containers that replace the previous Team/Workspace split.
 * Each space has exactly 1 moderator (owner) and 0+ members.
 */

/**
 * User information for display purposes
 */
export interface SpaceUser {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
}

/**
 * Basic Space entity from the API
 */
export interface Space {
  id: string;
  name: string;
  moderatorUserId: string;
  memberIds: string[];
  createdAt: string;
  updatedAt: string;
}

/**
 * Space with enriched user details for display
 */
export interface SpaceWithDetails extends Space {
  moderator: SpaceUser;
  members: SpaceUser[];
}

/**
 * Request payload for creating a new space
 */
export interface CreateSpaceRequest {
  name: string;
  moderatorUserId: string;
  memberIds?: string[];
}

/**
 * Request payload for updating a space's name
 */
export interface UpdateSpaceNameRequest {
  name: string;
}

/**
 * Request payload for changing a space's moderator
 */
export interface ChangeSpaceModeratorRequest {
  newModeratorUserId: string;
}

/**
 * Request payload for adding a member to a space
 */
export interface AddSpaceMemberRequest {
  userId: string;
}

/**
 * Request payload for removing a member from a space
 */
export interface RemoveSpaceMemberRequest {
  userId: string;
}

/**
 * Response from the spaces list endpoint
 */
export interface SpaceListResponse {
  items: Space[];
  total: number;
  skip: number;
  take: number;
}

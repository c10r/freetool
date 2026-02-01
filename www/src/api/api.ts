import createClient, { type Middleware } from "openapi-fetch";
import type { KeyValuePair } from "@/features/space/types";
import type { paths } from "../schema";
import { handleApiError } from "./errorHandler";

/**
 * Dev Mode Support
 */
const DEV_USER_ID_KEY = "freetool_dev_user_id";

export function isDevModeEnabled(): boolean {
  const value = import.meta.env.VITE_DEV_MODE;
  const result = value === "true";
  return result;
}

export function getDevUserId(): string | null {
  if (!isDevModeEnabled()) {
    return null;
  }
  const userId = localStorage.getItem(DEV_USER_ID_KEY);
  return userId;
}

export function setDevUserId(userId: string): void {
  if (!isDevModeEnabled()) {
    return;
  }
  localStorage.setItem(DEV_USER_ID_KEY, userId);
}

/**
 * Dev User type for the user switcher
 */
export interface DevUser {
  id: string;
  name: string;
  email: string;
}

/**
 * Fetch dev users from the backend
 * Note: We fetch directly from the backend to avoid Vite proxy chunked encoding issues
 */
export async function getDevUsers(): Promise<DevUser[]> {
  // In Docker, use the internal network; locally, use localhost:5002
  const backendUrl =
    typeof window !== "undefined" && window.location.port === "8081"
      ? "http://localhost:5002" // Direct to backend, bypassing proxy
      : `${window.location.origin}/freetool`;
  const url = `${backendUrl}/dev/users`;
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error("Failed to fetch dev users");
  }
  const users = await response.json();
  return users;
}

const client = createClient<paths>({
  baseUrl: `${typeof window !== "undefined" ? window.location.origin : "http://localhost:5000"}/freetool`,
});

// Dev mode middleware - adds X-Dev-User-Id header when in dev mode
const devModeMiddleware: Middleware = {
  onRequest({ request }) {
    if (isDevModeEnabled()) {
      const devUserId = getDevUserId();
      if (devUserId) {
        request.headers.set("X-Dev-User-Id", devUserId);
      }
    }
    return request;
  },
};

// Error handling middleware - shows toast notifications for API errors
const errorMiddleware: Middleware = {
  async onResponse({ response }) {
    if (!response.ok) {
      let serverMessage: string | undefined;

      try {
        // Clone response to avoid consuming the body
        const body = await response.clone().json();
        // Extract message from ProblemDetails or generic error response
        serverMessage = body?.detail || body?.title || body?.message;
      } catch {
        // Response body is not JSON or already consumed - use status-based message
      }

      handleApiError(response.status, serverMessage);
    }
    return response;
  },
};

// Register middleware
client.use(devModeMiddleware);
client.use(errorMiddleware);

/**
 * App
 */

type AppInput = {
  input: {
    title: string;
    description?: string | null;
    type: string;
  };
  required: boolean;
  defaultValue?: string;
};

type AppCreateInput = {
  name: string;
  description?: string | null;
  folderId: string | null;
  resourceId: string;
  inputs: AppInput[];
  urlPath?: string;
  body: KeyValuePair[];
  headers: KeyValuePair[];
  urlParameters: KeyValuePair[];
  useDynamicJsonBody?: boolean;
};

export const getApps = (skip?: number, take?: number) => {
  if (typeof skip === "number" && typeof take === "number") {
    return client.GET("/app", {
      params: {
        query: { skip, take },
      },
    });
  }
  return client.GET("/app");
};

export const getAppById = (appId: string) => {
  return client.GET("/app/{id}", {
    params: {
      path: { id: appId },
    },
  });
};

export const getAppByFolder = (
  folderId: string,
  skip?: number,
  take?: number
) => {
  if (typeof skip === "number" && typeof take === "number") {
    return client.GET("/app/folder/{folderId}", {
      params: {
        path: { folderId },
        query: { skip, take },
      },
    });
  }
  return client.GET("/app/folder/{folderId}", {
    params: {
      path: { folderId },
    },
  });
};

export const createApp = (appInputs: AppCreateInput) => {
  return client.POST("/app", {
    body: appInputs,
  });
};

export const deleteApp = (appId: string) => {
  return client.DELETE("/app/{id}", {
    params: {
      path: { id: appId },
    },
  });
};

export const runApp = (
  appId: string,
  inputValues?: { title: string; value: string }[],
  dynamicBody?: { key: string; value: string }[]
) => {
  return client.POST("/app/{id}/run", {
    params: {
      path: { id: appId },
    },
    body: {
      inputValues: inputValues ?? [],
      dynamicBody: dynamicBody,
    },
  });
};

export const updateAppUseDynamicJsonBody = (
  appId: string,
  useDynamicJsonBody: boolean
) => {
  return client.PUT("/app/{id}/use-dynamic-json-body", {
    params: {
      path: { id: appId },
    },
    body: {
      useDynamicJsonBody,
    },
  });
};

export const updateAppInputSchema = (
  appId: string,
  inputs: {
    input: { title: string; description?: string | null; type: unknown };
    required: boolean;
    defaultValue?: string;
  }[]
) => {
  return client.PUT("/app/{id}/inputs", {
    params: { path: { id: appId } },
    body: { inputs },
  });
};

export const updateAppInputs = (
  appId: string,
  body?: KeyValuePair[],
  headers?: KeyValuePair[],
  urlParameters?: KeyValuePair[]
) => {
  return client.PUT("/app/{id}/inputs", {
    params: {
      path: { id: appId },
    },
    body: {
      body,
      headers,
      urlParameters,
    },
  });
};

export const updateAppName = (appId: string, name: string) => {
  return client.PUT("/app/{id}/name", {
    params: {
      path: { id: appId },
    },
    body: {
      name,
    },
  });
};

export const updateAppBody = (appId: string, body: KeyValuePair[]) => {
  return client.PUT("/app/{id}/body", {
    params: {
      path: { id: appId },
    },
    body: {
      body,
    },
  });
};

export const updateAppHeaders = (appId: string, headers: KeyValuePair[]) => {
  return client.PUT("/app/{id}/headers", {
    params: {
      path: { id: appId },
    },
    body: {
      headers,
    },
  });
};

export const updateAppQueryParams = (
  appId: string,
  urlParameters: KeyValuePair[]
) => {
  return client.PUT("/app/{id}/query-parameters", {
    params: {
      path: { id: appId },
    },
    body: {
      urlParameters,
    },
  });
};

export const updateAppUrlPath = (appId: string, urlPath: string) => {
  return client.PUT("/app/{id}/url-path", {
    params: {
      path: { id: appId },
    },
    body: {
      urlPath,
    },
  });
};

export const updateAppHttpMethod = (appId: string, httpMethod: string) => {
  return client.PUT("/app/{id}/http-method", {
    params: {
      path: { id: appId },
    },
    body: {
      httpMethod,
    },
  });
};

export const updateAppDescription = (
  appId: string,
  description: string | null
) => {
  return client.PUT("/app/{id}/description", {
    params: {
      path: { id: appId },
    },
    body: {
      description,
    },
  });
};

/**
 * Audit Log
 */

export const getAuditEvents = (skip?: number, take?: number) => {
  if (skip && take) {
    return client.GET("/audit/events", {
      params: {
        query: { skip, take },
      },
    });
  }
  return client.GET("/audit/events");
};

/**
 * Folders
 */

export const createFolder = (
  name: string,
  parentId: string | null,
  spaceId: string
) => {
  return client.POST("/folder", {
    body: {
      name,
      location: parentId,
      spaceId,
    },
  });
};

export const getAllFolders = (skip?: number, take?: number) => {
  if (typeof skip === "number" && typeof take === "number") {
    return client.GET("/folder", {
      params: {
        query: {
          skip,
          take,
        },
      },
    });
  }
  return client.GET("/folder");
};

export const getFoldersBySpace = (
  spaceId: string,
  skip?: number,
  take?: number
) => {
  if (typeof skip === "number" && typeof take === "number") {
    return client.GET("/folder", {
      params: {
        query: {
          spaceId,
          skip,
          take,
        },
      },
    });
  }
  return client.GET("/folder", {
    params: {
      query: {
        spaceId,
      },
    },
  });
};

export const getFolderChildren = (id: string) => {
  return client.GET("/folder/{id}/children", {
    params: { path: { id } },
  });
};

export const getFolderById = (id: string) => {
  return client.GET("/folder/{id}", {
    params: {
      path: { id },
    },
  });
};

export const getRootFolder = (skip?: number, take?: number) => {
  if (typeof skip === "number" && typeof take === "number") {
    return client.GET("/folder/root", {
      params: {
        query: { skip, take },
      },
    });
  }
  return client.GET("/folder/root");
};

export const deleteFolder = (id: string) => {
  return client.DELETE("/folder/{id}", {
    params: {
      path: { id },
    },
  });
};

export const moveFolder = (id: string, parentFolderId: string | null) => {
  return client.PUT("/folder/{id}/move", {
    params: {
      path: { id },
    },
    body: {
      parentId: parentFolderId !== null ? parentFolderId : undefined,
    },
  });
};

export const updateFolderName = (id: string, newName: string) => {
  return client.PUT("/folder/{id}/name", {
    params: {
      path: { id },
    },
    body: {
      name: newName,
    },
  });
};

/**
 * Spaces
 *
 * Spaces are unified containers that replace the previous Group/Workspace split.
 * Each space has exactly 1 moderator and 0+ members.
 */

export const getSpaces = (skip?: number, take?: number) => {
  if (typeof skip === "number" && typeof take === "number") {
    return client.GET("/space", {
      params: {
        query: { skip, take },
      },
    });
  }
  return client.GET("/space");
};

export const getSpaceById = (spaceId: string) => {
  return client.GET("/space/{id}", {
    params: {
      path: { id: spaceId },
    },
  });
};

export const createSpace = ({
  name,
  moderatorUserId,
  memberIds,
}: {
  name: string;
  moderatorUserId: string;
  memberIds?: string[];
}) => {
  return client.POST("/space", {
    body: {
      name,
      moderatorUserId,
      memberIds: memberIds || [],
    },
  });
};

export const deleteSpace = (spaceId: string) => {
  return client.DELETE("/space/{id}", {
    params: {
      path: { id: spaceId },
    },
  });
};

export const updateSpaceName = ({ id, name }: { id: string; name: string }) => {
  return client.PUT("/space/{id}/name", {
    params: {
      path: { id },
    },
    body: {
      name,
    },
  });
};

export const changeSpaceModerator = ({
  spaceId,
  newModeratorUserId,
}: {
  spaceId: string;
  newModeratorUserId: string;
}) => {
  return client.PUT("/space/{id}/moderator", {
    params: {
      path: { id: spaceId },
    },
    body: {
      newModeratorUserId,
    },
  });
};

export const addSpaceMember = ({
  spaceId,
  userId,
}: {
  spaceId: string;
  userId: string;
}) => {
  return client.POST("/space/{id}/members", {
    params: {
      path: { id: spaceId },
    },
    body: {
      userId,
    },
  });
};

export const removeSpaceMember = ({
  spaceId,
  userId,
}: {
  spaceId: string;
  userId: string;
}) => {
  return client.DELETE("/space/{id}/members/{memberId}", {
    params: {
      path: { id: spaceId, memberId: userId },
    },
  });
};

/**
 * Resources
 */

export const getResources = (spaceId: string, skip?: number, take?: number) => {
  if (skip !== undefined && take !== undefined) {
    return client.GET("/space/{spaceId}/resource", {
      params: {
        path: { spaceId },
        query: { skip, take },
      },
    });
  }

  return client.GET("/space/{spaceId}/resource", {
    params: {
      path: { spaceId },
    },
  });
};

type CreateHttpResourceInput = {
  spaceId: string;
  name: string;
  description: string;
  resourceKind: "http";
  baseUrl: string;
  urlParameters: { key: string; value: string }[];
  headers: { key: string; value: string }[];
  body: { key: string; value: string }[];
};

type CreateSqlResourceInput = {
  spaceId: string;
  name: string;
  description: string;
  resourceKind: "sql";
  databaseName: string;
  databaseHost: string;
  databasePort: number;
  databaseAuthScheme: "username_password";
  databaseUsername: string;
  databasePassword?: string;
  useSsl: boolean;
  enableSshTunnel: boolean;
  connectionOptions: { key: string; value: string }[];
};

export const createResource = (
  input: CreateHttpResourceInput | CreateSqlResourceInput
) => {
  if (input.resourceKind === "http") {
    const {
      spaceId,
      name,
      description,
      baseUrl,
      urlParameters,
      headers,
      body,
    } = input;
    return client.POST("/resource/http", {
      body: {
        spaceId,
        name,
        description,
        baseUrl,
        urlParameters,
        headers,
        body,
      },
    });
  }

  const {
    spaceId,
    name,
    description,
    databaseName,
    databaseHost,
    databasePort,
    databaseAuthScheme,
    databaseUsername,
    databasePassword,
    useSsl,
    enableSshTunnel,
    connectionOptions,
  } = input;

  return client.POST("/resource/sql", {
    body: {
      spaceId,
      name,
      description,
      databaseName,
      databaseHost,
      databasePort,
      databaseAuthScheme,
      databaseUsername,
      databasePassword,
      useSsl,
      enableSshTunnel,
      connectionOptions,
    },
  });
};

export const deleteResource = (id: string) => {
  return client.DELETE("/resource/{id}", {
    params: {
      path: { id },
    },
  });
};

export const updateResourceName = ({
  id,
  newName,
}: {
  id: string;
  newName: string;
}) => {
  return client.PUT("/resource/{id}/name", {
    params: {
      path: {
        id,
      },
    },
    body: {
      name: newName,
    },
  });
};

export const updateResourceDescription = ({
  id,
  newDescription,
}: {
  id: string;
  newDescription: string;
}) => {
  return client.PUT("/resource/{id}/description", {
    params: {
      path: {
        id,
      },
    },
    body: {
      description: newDescription,
    },
  });
};

export const updateResourceBaseUrl = ({
  id,
  baseUrl,
}: {
  id: string;
  baseUrl: string;
}) => {
  return client.PUT("/resource/{id}/base-url", {
    params: {
      path: {
        id,
      },
    },
    body: {
      baseUrl,
    },
  });
};

export const updateResourceUrlParameters = ({
  id,
  urlParameters,
}: {
  id: string;
  urlParameters: { key: string; value: string }[];
}) => {
  return client.PUT("/resource/{id}/url-parameters", {
    params: {
      path: {
        id,
      },
    },
    body: {
      urlParameters,
    },
  });
};

export const updateResourceHeaders = ({
  id,
  headers,
}: {
  id: string;
  headers: { key: string; value: string }[];
}) => {
  return client.PUT("/resource/{id}/headers", {
    params: {
      path: {
        id,
      },
    },
    body: {
      headers,
    },
  });
};

export const updateResourceBody = ({
  id,
  body,
}: {
  id: string;
  body: { key: string; value: string }[];
}) => {
  return client.PUT("/resource/{id}/body", {
    params: {
      path: {
        id,
      },
    },
    body: {
      body,
    },
  });
};

export const updateResourceDatabaseConfig = ({
  id,
  databaseName,
  databaseHost,
  databasePort,
  databaseAuthScheme,
  databaseUsername,
  databasePassword,
  useSsl,
  enableSshTunnel,
  connectionOptions,
}: {
  id: string;
  databaseName: string;
  databaseHost: string;
  databasePort: number;
  databaseAuthScheme: "username_password";
  databaseUsername: string;
  databasePassword?: string;
  useSsl: boolean;
  enableSshTunnel: boolean;
  connectionOptions: { key: string; value: string }[];
}) => {
  return client.PUT("/resource/{id}/database-config", {
    params: {
      path: {
        id,
      },
    },
    body: {
      databaseName,
      databaseHost,
      databasePort,
      databaseAuthScheme,
      databaseUsername,
      databasePassword,
      useSsl,
      enableSshTunnel,
      connectionOptions,
    },
  });
};

/**
 * Users
 */

export const getUsers = (skip?: number, take?: number) => {
  if (typeof skip === "number" && typeof take === "number") {
    return client.GET("/user", {
      params: {
        query: { skip, take },
      },
    });
  }
  return client.GET("/user");
};

export const inviteUser = ({ email }: { email: string }) =>
  client.POST("/user/invite", { body: { email } });

/**
 * Authorization & Permissions
 *
 * NOTE: Space permissions is still a placeholder until GET /space/{id}/permissions
 * is implemented in the backend.
 */

import type {
  CurrentUserResponse,
  SpacePermissions,
  SpacePermissionsResponse,
} from "@/types/permissions";

/**
 * Get the current authenticated user's profile and role information
 *
 * @returns Current user data including spaces and admin status
 */
export const getCurrentUser = (): Promise<{
  data?: CurrentUserResponse;
  error?: { message: string };
}> => client.GET("/user/me");

/**
 * Get the current user's permissions for a specific space
 *
 * @param spaceId - The ID of the space to check permissions for
 * @returns Permissions object with all space-level permissions
 *
 * NOTE: This is a PLACEHOLDER implementation. The backend endpoint
 * GET /space/{id}/permissions does not exist yet. This returns mock data.
 */
export const getSpacePermissions = async (
  spaceId: string
): Promise<{
  data?: SpacePermissionsResponse;
  error?: { message: string };
}> => {
  // Simulate API delay
  await new Promise((resolve) => setTimeout(resolve, 100));

  return {
    data: {
      spaceId,
      userId: "user-1",
      permissions: {
        create_resource: true,
        edit_resource: true,
        delete_resource: true,
        create_app: true,
        edit_app: true,
        delete_app: true,
        run_app: true,
        create_folder: true,
        edit_folder: true,
        delete_folder: true,
      },
      isOrgAdmin: true,
      isSpaceModerator: true,
    },
  };
};

/**
 * Trash / Restore
 */
export const getTrashBySpace = (
  spaceId: string,
  skip?: number,
  take?: number
) => {
  if (skip !== undefined && take !== undefined) {
    return client.GET("/trash/space/{spaceId}", {
      params: {
        path: { spaceId },
        query: { skip, take },
      },
    });
  }
  return client.GET("/trash/space/{spaceId}", {
    params: {
      path: { spaceId },
    },
  });
};

export const restoreApp = (appId: string) => {
  return client.POST("/trash/app/{appId}/restore", {
    params: { path: { appId } },
  });
};

export const restoreFolder = (folderId: string) => {
  return client.POST("/trash/folder/{folderId}/restore", {
    params: { path: { folderId } },
  });
};

export const restoreResource = (resourceId: string) => {
  return client.POST("/trash/resource/{resourceId}/restore", {
    params: { path: { resourceId } },
  });
};

/**
 * Space Permissions Management
 */

/**
 * Get all members and their permissions for a space
 *
 * @param spaceId - The ID of the space
 * @param skip - Number of items to skip (for pagination)
 * @param take - Number of items to take (for pagination)
 * @returns All members with their permissions
 */
export const getSpaceMembersPermissions = (
  spaceId: string,
  skip?: number,
  take?: number
) => {
  if (skip !== undefined && take !== undefined) {
    return client.GET("/space/{id}/permissions", {
      params: {
        path: { id: spaceId },
        query: { skip, take },
      },
    });
  }
  return client.GET("/space/{id}/permissions", {
    params: { path: { id: spaceId } },
  });
};

/**
 * Update a user's permissions in a space
 *
 * @param spaceId - The ID of the space
 * @param userId - The ID of the user to update
 * @param permissions - The new permissions to set
 * @returns Success or error
 */
export const updateUserPermissions = ({
  spaceId,
  userId,
  permissions,
}: {
  spaceId: string;
  userId: string;
  permissions: SpacePermissions;
}) => {
  return client.PUT("/space/{id}/permissions", {
    params: { path: { id: spaceId } },
    body: { userId, permissions },
  });
};

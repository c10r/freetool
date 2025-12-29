import createClient, { type Middleware } from "openapi-fetch";
import type { KeyValuePair } from "@/features/space/types";
import type { paths } from "../schema";
import { handleApiError } from "./errorHandler";

const client = createClient<paths>({
  baseUrl: `${typeof window !== "undefined" ? window.location.origin : "http://localhost:5000"}/freetool`,
});

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
client.use(errorMiddleware);

/**
 * App
 */

type AppInput = {
  inputs: {
    title: string;
    type: string;
  };
  required: boolean;
};

type AppCreateInput = {
  name: string;
  folderId: string | null;
  resourceId: string;
  inputs: AppInput[];
  urlPath?: string;
  body: KeyValuePair[];
  headers: KeyValuePair[];
  urlParameters: KeyValuePair[];
};

export const getApps = (skip?: number, take?: number) => {
  if (skip && take) {
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
  if (skip && take) {
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

export const runApp = (appId: string) => {
  return client.POST("/app/{id}/run", {
    params: {
      path: { id: appId },
    },
    body: {
      inputValues: [],
    },
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
  if (skip && take) {
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
  if (skip && take) {
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
  if (skip && take) {
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
  if (skip && take) {
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

export const getResources = (skip?: number, take?: number) => {
  if (skip && take) {
    return client.GET("/resource", {
      params: {
        query: { skip, take },
      },
    });
  }

  return client.GET("/resource");
};

export const createResource = ({
  spaceId,
  name,
  description,
  baseUrl,
  httpMethod,
  urlParameters,
  headers,
  body,
}: {
  spaceId: string;
  name: string;
  description: string;
  baseUrl: string;
  httpMethod: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  urlParameters: { key: string; value: string }[];
  headers: { key: string; value: string }[];
  body: { key: string; value: string }[];
}) => {
  return client.POST("/resource", {
    body: {
      spaceId,
      name,
      description,
      baseUrl,
      httpMethod,
      urlParameters,
      headers,
      body,
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

/**
 * Users
 */

export const getUsers = (skip?: number, take?: number) => {
  if (skip && take) {
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
 * NOTE: These endpoints are NOT yet implemented in the backend.
 * They return placeholder data for frontend development.
 * TODO: Once backend implements GET /user/me and GET /space/{id}/permissions,
 * update these functions to use the real endpoints.
 */

import type {
  CurrentUserResponse,
  SpacePermissionsResponse,
} from "@/types/permissions";

/**
 * Get the current authenticated user's profile and role information
 *
 * @returns Current user data including spaces and admin status
 *
 * NOTE: This is a PLACEHOLDER implementation. The backend endpoint
 * GET /user/me does not exist yet. This returns mock data.
 */
export const getCurrentUser = async (): Promise<{
  data?: CurrentUserResponse;
  error?: { message: string };
}> => {
  // Simulate API delay
  await new Promise((resolve) => setTimeout(resolve, 100));

  return {
    data: {
      id: "user-1",
      name: "Development User",
      email: "dev@example.com",
      profilePicUrl: undefined,
      isOrgAdmin: true, // Mock as org admin for development
      spaces: [
        {
          id: "space-1",
          name: "Engineering",
          role: "moderator",
        },
      ],
    },
  };
};

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
export const getTrashBySpace = (spaceId: string) => {
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

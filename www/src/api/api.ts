import createClient from "openapi-fetch";
import type { paths } from "../schema";

const client = createClient<paths>({
  baseUrl: "https://chanders-macbook-pro.koala-snake.ts.net:8443",
});

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

export const createFolder = (name: string, parentId: string | null) => {
  return client.POST("/folder", {
    body: {
      name,
      location: parentId,
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

export const getFolderChildren = (id: string) => {
  return client.GET("/folder/{id}/children", {
    params: { path: { id } },
  });
};

export const getRootFolder = (skip?: number, take?: number) => {
  if (skip && take) {
    return client.GET("/folder/root", {
      parmas: {
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
 * Groups
 */

export const getGroups = (skip?: number, take?: number) => {
  if (skip && take) {
    return client.GET("/group", {
      params: {
        query: { skip, take },
      },
    });
  }
  return client.GET("/group");
};

export const createGroup = ({
  name,
  userIds,
}: {
  name: string;
  userIds: string[];
}) => {
  return client.POST("/group", {
    body: {
      name,
      userIds,
    },
  });
};

export const deleteGroup = (groupId: string) => {
  return client.DELETE("/group/{id}", {
    params: {
      path: { id: groupId },
    },
  });
};

export const updateGroupName = ({ id, name }: { id: string; name: string }) => {
  return client.PUT("/group/{id}/name", {
    params: {
      path: {
        id,
      },
    },
    body: {
      name,
    },
  });
};

export const addGroupMember = ({
  groupId,
  userId,
}: {
  groupId: string;
  userId: string;
}) => {
  return client.POST("/group/{id}/users", {
    params: {
      path: {
        id: groupId,
      },
    },
    body: {
      userId,
    },
  });
};

export const removeGroupMember = ({
  groupId,
  userId,
}: {
  groupId: string;
  userId: string;
}) => {
  return client.DELETE("/group/{id}/users", {
    params: {
      path: {
        id: groupId,
      },
    },
    body: {
      userId,
    },
  });
};

export const getGroupById = (groupId: string) => {
  return client.GET("/group/{id}", {
    params: {
      path: { id: groupId },
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
  name,
  description,
  baseUrl,
  httpMethod,
  urlParameters,
  headers,
  body,
}: {
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

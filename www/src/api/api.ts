import createClient from "openapi-fetch";
import type { paths } from "../schema";

const client = createClient<paths>({
  baseUrl: "https://chanders-macbook-pro.koala-snake.ts.net:8443",
});

export const getAuditEvents = (skip?: number, take?: number) => {
  if (skip && take) {
    return client.GET("/audit/events", {
      params: {
        query: { "Skip.Value": skip, "Take.Value": take },
      },
    });
  }
  return client.GET("/audit/events");
};

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

export const deleteResource = (id: string) => {
  return client.DELETE("/resource/{id}", {
    params: {
      path: { id },
    },
  });
};

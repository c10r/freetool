import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getAllFolders, getApps, getSpaces, getUsers } from "@/api/api";
import type {
  AppNode,
  EndpointMethod,
  FolderNode,
  SpaceNode,
} from "@/features/space/types";
import {
  extractDefaultValue,
  fromBackendInputType,
  getRadioOptionsFromBackendType,
} from "@/lib/inputTypeMapper";
import { DEFAULT_PAGE_SIZE } from "@/lib/pagination";
import type { components } from "@/schema";
import type { Space, SpaceUser, SpaceWithDetails } from "@/types/space";

type InputType = components["schemas"]["InputType"];

// Constants for pagination
const MAX_PAGE_SIZE = DEFAULT_PAGE_SIZE;

// Query keys for cache invalidation
export const sidebarQueryKeys = {
  all: ["sidebar"] as const,
  tree: () => [...sidebarQueryKeys.all, "tree"] as const,
};

interface SidebarTreeData {
  nodes: Record<string, SpaceNode>;
  spaces: SpaceWithDetails[];
  rootId: string;
}

interface PaginatedResponse<T> {
  items?: T[] | null;
  totalCount?: number;
  skip?: number;
  take?: number;
}

// Helper to fetch all pages of a paginated endpoint
async function fetchAllPages<T>(
  fetcher: (
    skip: number,
    take: number
  ) => Promise<{ data?: PaginatedResponse<T> | null; error?: unknown }>
): Promise<T[]> {
  const allItems: T[] = [];
  let skip = 0;
  let hasMore = true;

  while (hasMore) {
    const response = await fetcher(skip, MAX_PAGE_SIZE);
    if (response.error || !response.data?.items) {
      break;
    }

    const pageItems = response.data.items;
    if (pageItems.length === 0) {
      break;
    }

    allItems.push(...pageItems);

    const totalCount = response.data.totalCount;
    const pageSize = response.data.take ?? pageItems.length;
    skip += pageItems.length;
    hasMore =
      totalCount !== undefined
        ? skip < totalCount
        : pageItems.length === pageSize;
  }

  return allItems;
}

// API response types
interface ApiFolderData {
  id?: string;
  name?: string;
  parentId?: string | null;
  spaceId?: string;
}

interface ApiAppData {
  id?: string;
  name?: string;
  folderId?: string | null;
  resourceId?: string;
  httpMethod?: string;
  urlPath?: string;
  urlParameters?: { key?: string; value?: string }[];
  headers?: { key?: string; value?: string }[];
  body?: { key?: string; value?: string }[];
  inputs?: {
    title?: string | null;
    type?: unknown;
    required?: boolean;
    defaultValue?: unknown;
  }[];
}

interface ApiUserData {
  id: string;
  name: string;
  email: string;
  profilePicUrl?: string;
}

export function useSidebarTree() {
  const queryClient = useQueryClient();

  const { data, isLoading, error, refetch } = useQuery<SidebarTreeData>({
    queryKey: sidebarQueryKeys.tree(),
    queryFn: async () => {
      // Fetch all data in parallel
      const [allSpaces, allFolders, allApps, allUsers] = await Promise.all([
        fetchAllPages<Space>((skip, take) => getSpaces(skip, take)),
        fetchAllPages<ApiFolderData>((skip, take) => getAllFolders(skip, take)),
        fetchAllPages<ApiAppData>((skip, take) => getApps(skip, take)),
        fetchAllPages<ApiUserData>((skip, take) => getUsers(skip, take)),
      ]);

      // Build users map for space enrichment
      const usersMap = new Map<string, SpaceUser>();
      for (const user of allUsers) {
        usersMap.set(user.id, {
          id: user.id,
          name: user.name,
          email: user.email,
          profilePicUrl: user.profilePicUrl,
        });
      }

      // Build the nodes map
      const nodes: Record<string, SpaceNode> = {};
      const rootId = "root";

      // Create virtual root node
      const root: FolderNode = {
        id: rootId,
        name: "Spaces",
        type: "folder",
        childrenIds: [],
      };
      nodes[rootId] = root;

      // First pass: Create all folder nodes
      for (const folder of allFolders) {
        if (folder.id) {
          const folderNode: FolderNode = {
            id: folder.id,
            name: folder.name || "Unnamed Folder",
            type: "folder",
            parentId: folder.parentId ?? rootId,
            childrenIds: [],
            spaceId: folder.spaceId,
          };
          nodes[folder.id] = folderNode;
        }
      }

      // Second pass: Create all app nodes
      for (const app of allApps) {
        if (app.id) {
          const appNode: AppNode = {
            id: app.id,
            name: app.name || "Unnamed App",
            type: "app",
            parentId: app.folderId || rootId,
            fields: (app.inputs || []).map((input) => {
              const fieldType = fromBackendInputType(input.type as InputType);
              const isBoolean = fieldType === "boolean";
              return {
                id: crypto.randomUUID(),
                label: input.title || "",
                description: input.description || undefined,
                type: fieldType,
                required: isBoolean ? true : (input.required ?? false),
                options: getRadioOptionsFromBackendType(
                  input.type as InputType
                ),
                defaultValue: isBoolean
                  ? undefined
                  : extractDefaultValue(input.defaultValue) || undefined,
              };
            }),
            resourceId: app.resourceId || undefined,
            httpMethod: (app.httpMethod as EndpointMethod) || undefined,
            urlPath: app.urlPath || "",
            urlParameters: (app.urlParameters || []).map((kvp) => ({
              key: kvp.key || "",
              value: kvp.value || "",
            })),
            headers: (app.headers || []).map((kvp) => ({
              key: kvp.key || "",
              value: kvp.value || "",
            })),
            body: (app.body || []).map((kvp) => ({
              key: kvp.key || "",
              value: kvp.value || "",
            })),
            useDynamicJsonBody: app.useDynamicJsonBody ?? false,
            sqlConfig: app.sqlConfig
              ? {
                  mode: app.sqlConfig.mode,
                  table: app.sqlConfig.table || undefined,
                  columns: app.sqlConfig.columns || [],
                  filters: (app.sqlConfig.filters || []).map((filter) => ({
                    column: filter.column || "",
                    operator: filter.operator,
                    value: filter.value || undefined,
                  })),
                  limit: app.sqlConfig.limit || undefined,
                  orderBy: (app.sqlConfig.orderBy || []).map((order) => ({
                    column: order.column || "",
                    direction: order.direction,
                  })),
                  rawSql: app.sqlConfig.rawSql || undefined,
                  rawSqlParams: (app.sqlConfig.rawSqlParams || []).map(
                    (kvp) => ({
                      key: kvp.key || "",
                      value: kvp.value || "",
                    })
                  ),
                }
              : undefined,
            description: app.description || undefined,
          };
          nodes[app.id] = appNode;
        }
      }

      // Third pass: Build parent-child relationships
      for (const node of Object.values(nodes)) {
        if (node.id === rootId) {
          continue;
        }

        const parentId = node.parentId || rootId;
        const parent = nodes[parentId];

        if (parent && parent.type === "folder") {
          if (!parent.childrenIds.includes(node.id)) {
            parent.childrenIds.push(node.id);
          }
        }
      }

      // Enrich spaces with user details
      const spaces: SpaceWithDetails[] = allSpaces.map((space) => {
        const moderator = usersMap.get(space.moderatorUserId) || {
          id: space.moderatorUserId,
          name: "Unknown User",
          email: "",
        };

        const members: SpaceUser[] = space.memberIds
          .map((memberId) => usersMap.get(memberId))
          .filter((user): user is SpaceUser => user !== undefined);

        return {
          ...space,
          moderator,
          members,
        };
      });

      return { nodes, spaces, rootId };
    },
    staleTime: 5 * 60 * 1000, // 5 minutes - same as useSpaces
    gcTime: 10 * 60 * 1000, // 10 minutes cache time
  });

  // Invalidation function for mutations
  const invalidateTree = () => {
    queryClient.invalidateQueries({ queryKey: sidebarQueryKeys.tree() });
  };

  return {
    nodes: data?.nodes ?? {},
    spaces: data?.spaces ?? [],
    rootId: data?.rootId ?? "root",
    isLoading,
    error,
    refetch,
    invalidateTree,
  };
}

// Hook for invalidating sidebar from mutation callbacks
export function useInvalidateSidebar() {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: sidebarQueryKeys.tree() });
  };

  return {
    invalidateOnFolderChange: invalidate,
    invalidateOnAppChange: invalidate,
    invalidateOnResourceChange: invalidate,
    invalidateOnSpaceChange: invalidate,
  };
}

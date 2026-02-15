import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getDefaultMemberPermissions,
  updateDefaultMemberPermissions,
} from "@/api/api";
import type { SpacePermissions } from "@/types/permissions";

function mapPermissionsFromApi(
  apiPermissions:
    | {
        createResource?: boolean;
        editResource?: boolean;
        deleteResource?: boolean;
        createApp?: boolean;
        editApp?: boolean;
        deleteApp?: boolean;
        runApp?: boolean;
        createFolder?: boolean;
        editFolder?: boolean;
        deleteFolder?: boolean;
        createDashboard?: boolean;
        editDashboard?: boolean;
        deleteDashboard?: boolean;
        runDashboard?: boolean;
      }
    | null
    | undefined
): SpacePermissions {
  return {
    create_resource: apiPermissions?.createResource ?? false,
    edit_resource: apiPermissions?.editResource ?? false,
    delete_resource: apiPermissions?.deleteResource ?? false,
    create_app: apiPermissions?.createApp ?? false,
    edit_app: apiPermissions?.editApp ?? false,
    delete_app: apiPermissions?.deleteApp ?? false,
    run_app: apiPermissions?.runApp ?? false,
    create_folder: apiPermissions?.createFolder ?? false,
    edit_folder: apiPermissions?.editFolder ?? false,
    delete_folder: apiPermissions?.deleteFolder ?? false,
    create_dashboard: apiPermissions?.createDashboard ?? false,
    edit_dashboard: apiPermissions?.editDashboard ?? false,
    delete_dashboard: apiPermissions?.deleteDashboard ?? false,
    run_dashboard: apiPermissions?.runDashboard ?? false,
  };
}

function mapPermissionsToApi(permissions: SpacePermissions): {
  createResource: boolean;
  editResource: boolean;
  deleteResource: boolean;
  createApp: boolean;
  editApp: boolean;
  deleteApp: boolean;
  runApp: boolean;
  createFolder: boolean;
  editFolder: boolean;
  deleteFolder: boolean;
  createDashboard: boolean;
  editDashboard: boolean;
  deleteDashboard: boolean;
  runDashboard: boolean;
} {
  return {
    createResource: permissions.create_resource,
    editResource: permissions.edit_resource,
    deleteResource: permissions.delete_resource,
    createApp: permissions.create_app,
    editApp: permissions.edit_app,
    deleteApp: permissions.delete_app,
    runApp: permissions.run_app,
    createFolder: permissions.create_folder,
    editFolder: permissions.edit_folder,
    deleteFolder: permissions.delete_folder,
    createDashboard: permissions.create_dashboard,
    editDashboard: permissions.edit_dashboard,
    deleteDashboard: permissions.delete_dashboard,
    runDashboard: permissions.run_dashboard,
  };
}

interface UseSpaceDefaultMemberPermissionsResult {
  spaceName: string;
  permissions: SpacePermissions | null;
  isLoading: boolean;
  error: Error | null;
  refetch: () => void;
  updatePermissions: (permissions: SpacePermissions) => Promise<void>;
  isUpdating: boolean;
}

const PERMISSION_CACHE_TTL = 5 * 60 * 1000;

export function useSpaceDefaultMemberPermissions(
  spaceId: string
): UseSpaceDefaultMemberPermissionsResult {
  const queryClient = useQueryClient();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["spaceDefaultMemberPermissions", spaceId],
    queryFn: async () => {
      const result = await getDefaultMemberPermissions(spaceId);
      if (result.error || !result.data) {
        throw new Error(
          result.error?.message || "Failed to fetch default member permissions"
        );
      }

      return {
        spaceName: result.data.spaceName ?? "",
        permissions: mapPermissionsFromApi(result.data.permissions),
      };
    },
    staleTime: PERMISSION_CACHE_TTL,
    retry: 2,
    enabled: !!spaceId,
  });

  const mutation = useMutation({
    mutationFn: async (permissions: SpacePermissions) => {
      const result = await updateDefaultMemberPermissions({
        spaceId,
        permissions: mapPermissionsToApi(permissions) as SpacePermissions,
      });

      if (result.error) {
        throw new Error(result.error.message);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["spaceDefaultMemberPermissions", spaceId],
      });
      queryClient.invalidateQueries({
        queryKey: ["spaceMembersPermissions", spaceId],
      });
    },
  });

  const updatePermissions = async (permissions: SpacePermissions) => {
    await mutation.mutateAsync(permissions);
  };

  return {
    spaceName: data?.spaceName ?? "",
    permissions: data?.permissions ?? null,
    isLoading,
    error: error as Error | null,
    refetch,
    updatePermissions,
    isUpdating: mutation.isPending,
  };
}

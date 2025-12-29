import { Edit, FilePlus2, FolderPlus, Play, Plus, Trash2 } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  createFolder as createFolderAPI,
  deleteApp,
  deleteFolder as deleteFolderAPI,
  updateFolderName,
} from "@/api/api";
import { PermissionButton } from "@/components/PermissionButton";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Separator } from "@/components/ui/separator";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useHasPermission } from "@/hooks/usePermissions";
import { useResources } from "@/hooks/useResources";
import type { components } from "@/schema";
import type {
  FolderNode,
  KeyValuePair,
  WorkspaceMainProps,
  WorkspaceNode,
} from "../types";
import AppConfigForm from "./AppConfigForm";

export default function FolderView({
  folder,
  nodes,
  onSelect,
  createApp,
  updateNode,
  insertFolderNode,
  deleteNode,
  workspaceId,
}: WorkspaceMainProps & { folder: FolderNode }) {
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(folder.name);
  const effectiveWorkspaceId = folder.workspaceId || workspaceId || "";

  // Permission checks
  const _canCreateFolder = useHasPermission(
    effectiveWorkspaceId,
    "create_folder"
  );
  const _canEditFolder = useHasPermission(effectiveWorkspaceId, "edit_folder");
  const _canDeleteFolder = useHasPermission(
    effectiveWorkspaceId,
    "delete_folder"
  );
  const _canCreateApp = useHasPermission(effectiveWorkspaceId, "create_app");
  const _canDeleteApp = useHasPermission(effectiveWorkspaceId, "delete_app");

  // Check if resources exist (needed to enable/disable New App button)
  const { hasResources, isLoading: loadingResources } = useResources();

  // Update name when folder changes
  useEffect(() => {
    setName(folder.name);
    setEditing(false);
    setRenameError(null);
  }, [folder.name]);

  // Reset app creation form when navigating to a different folder
  useEffect(() => {
    setShowCreateAppForm(false);
    setCreateAppError(null);
    setAppFormData({
      name: "",
      resourceId: "",
      queryParameters: [],
      headers: [],
      body: [],
    });
  }, []);
  const [newFolderName, setNewFolderName] = useState("");
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);
  const [popoverOpen, setPopoverOpen] = useState(false);
  const [cardPopoverOpen, setCardPopoverOpen] = useState(false);
  const [newFolderParentId, setNewFolderParentId] = useState<string>(folder.id);
  const [createFolderError, setCreateFolderError] = useState<string | null>(
    null
  );
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [deletingChildId, setDeletingChildId] = useState<string | null>(null);
  const [deleteConfirmName, setDeleteConfirmName] = useState("");
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [isRenaming, setIsRenaming] = useState(false);
  const [renameError, setRenameError] = useState<string | null>(null);

  // App creation form state
  const [showCreateAppForm, setShowCreateAppForm] = useState(false);
  const [isCreatingApp, setIsCreatingApp] = useState(false);
  const [createAppError, setCreateAppError] = useState<string | null>(null);
  const [appFormData, setAppFormData] = useState({
    name: "",
    resourceId: "",
    urlPath: "",
    queryParameters: [] as KeyValuePair[],
    headers: [] as KeyValuePair[],
    body: [] as KeyValuePair[],
  });

  // App running state
  const [_runningAppId, _setRunningAppId] = useState<string | null>(null);
  const [_runResult, _setRunResult] = useState<
    components["schemas"]["RunData"] | null
  >(null);
  const [_runError, _setRunError] = useState<string | null>(null);

  const children = useMemo(
    () => folder.childrenIds.map((id) => nodes[id]).filter(Boolean),
    [folder.childrenIds, nodes]
  );

  const handleCreateFolder = async () => {
    const trimmedName = newFolderName.trim();
    if (!trimmedName) {
      return;
    }

    if (!effectiveWorkspaceId) {
      setCreateFolderError("Select a workspace before creating folders.");
      return;
    }

    const parentIdForApi =
      !newFolderParentId || newFolderParentId === "root"
        ? null
        : newFolderParentId;

    setIsCreatingFolder(true);
    setCreateFolderError(null);
    try {
      const response = await createFolderAPI(
        trimmedName,
        parentIdForApi,
        effectiveWorkspaceId
      );
      if (response.error) {
        setCreateFolderError(
          (response.error?.message as string) || "Failed to create folder"
        );
      } else if (response.data) {
        // Add the new folder to the local state using the API response
        const newFolder = response.data;
        const folderNode = {
          id: newFolder.id ?? "",
          name: newFolder.name,
          type: "folder" as const,
          parentId: newFolderParentId,
          childrenIds: [],
          workspaceId: effectiveWorkspaceId,
        };

        insertFolderNode(folderNode);

        // Reset form
        setNewFolderName("");
        setPopoverOpen(false);
        setCardPopoverOpen(false);
        setNewFolderParentId(folder.id);
      }
    } catch (error) {
      setCreateFolderError(
        error instanceof Error ? error.message : "Failed to create folder"
      );
    } finally {
      setIsCreatingFolder(false);
    }
  };

  const handleDeleteClick = (childId: string) => {
    setDeletingChildId(childId);
    setDeleteConfirmOpen(true);
    setDeleteConfirmName("");
    setDeleteError(null);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingChildId) {
      return;
    }

    const childToDelete = nodes[deletingChildId];
    if (!childToDelete || deleteConfirmName !== childToDelete.name) {
      setDeleteError("Folder name doesn't match");
      return;
    }

    setIsDeleting(true);
    setDeleteError(null);
    try {
      const response = await deleteFolderAPI(deletingChildId);
      if (response.error) {
        setDeleteError(
          (response.error?.message as string) || "Failed to delete folder"
        );
      } else {
        // Remove from local state
        deleteNode(deletingChildId);
        // Reset state
        setDeleteConfirmOpen(false);
        setDeletingChildId(null);
        setDeleteConfirmName("");
      }
    } catch (error) {
      setDeleteError(
        error instanceof Error ? error.message : "Failed to delete folder"
      );
    } finally {
      setIsDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteConfirmOpen(false);
    setDeletingChildId(null);
    setDeleteConfirmName("");
    setDeleteError(null);
  };

  const handleFolderCardPlusClick = (folderId: string) => {
    setNewFolderParentId(folderId);
    setCardPopoverOpen(true);
  };

  const handleRename = async () => {
    if (!name.trim() || name === folder.name) {
      setEditing(false);
      return;
    }

    setIsRenaming(true);
    setRenameError(null);
    try {
      const response = await updateFolderName(folder.id, name.trim());
      if (response.error) {
        setRenameError(
          (response.error?.message as string) || "Failed to rename folder"
        );
      } else {
        // Update local state
        updateNode({ ...folder, name: name.trim() });
        setEditing(false);
      }
    } catch (error) {
      setRenameError(
        error instanceof Error ? error.message : "Failed to rename folder"
      );
    } finally {
      setIsRenaming(false);
    }
  };

  const handleCreateApp = async () => {
    if (!appFormData.name.trim()) {
      setCreateAppError("App name is required");
      return;
    }

    setIsCreatingApp(true);
    setCreateAppError(null);

    try {
      // Call createApp with both name and resourceId
      await createApp(
        folder.id,
        appFormData.name.trim(),
        appFormData.resourceId,
        appFormData.urlPath,
        appFormData.queryParameters,
        appFormData.headers,
        appFormData.body
      );

      // Reset form and close on success
      setAppFormData({
        name: "",
        resourceId: "",
        urlPath: "",
        queryParameters: [],
        headers: [],
        body: [],
      });
      setShowCreateAppForm(false);
    } catch (error) {
      // Extract the actual error message from the thrown error
      const errorMessage =
        error instanceof Error
          ? error.message
          : "Failed to create app. Please try again.";
      setCreateAppError(errorMessage);
    } finally {
      setIsCreatingApp(false);
    }
  };

  const handleShowCreateAppForm = () => {
    setShowCreateAppForm(!showCreateAppForm);
  };

  const handleCancelCreateApp = () => {
    setShowCreateAppForm(false);
    setCreateAppError(null);
    setAppFormData({
      name: "",
      resourceId: "",
      queryParameters: [],
      headers: [],
      body: [],
    });
  };

  const handleRunApp = (appId: string) => {
    navigate(`/workspaces/${effectiveWorkspaceId}/${appId}/run`);
  };

  const handleCardClick = (child: WorkspaceNode) => {
    if (child.type === "app") {
      handleRunApp(child.id);
    } else {
      onSelect(child.id);
    }
  };

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          {editing ? (
            <div className="flex flex-col gap-2">
              <div className="flex items-center gap-2">
                <Input
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    if (renameError) {
                      setRenameError(null);
                    }
                  }}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      handleRename();
                    }
                    if (e.key === "Escape") {
                      setName(folder.name);
                      setEditing(false);
                      setRenameError(null);
                    }
                  }}
                  className="w-64"
                />
                <Button onClick={handleRename} disabled={isRenaming}>
                  {isRenaming ? "Saving..." : "Save"}
                </Button>
                <Button
                  variant="outline"
                  onClick={() => {
                    setName(folder.name);
                    setEditing(false);
                    setRenameError(null);
                  }}
                  disabled={isRenaming}
                >
                  Cancel
                </Button>
              </div>
              {renameError && (
                <p className="text-sm text-red-500">{renameError}</p>
              )}
            </div>
          ) : (
            <h2 className="text-2xl font-semibold">{folder.name}</h2>
          )}
          {folder.id !== "root" && (
            <PermissionButton
              workspaceId={effectiveWorkspaceId}
              permission="edit_folder"
              tooltipMessage="You don't have permission to rename folders. Contact your team admin."
              variant="secondary"
              size="icon"
              onClick={() => setEditing((v) => !v)}
              aria-label="Rename folder"
              hideWhenDisabled
            >
              <Edit size={16} />
            </PermissionButton>
          )}
        </div>
        <div className="flex gap-2">
          <Popover open={popoverOpen} onOpenChange={setPopoverOpen}>
            <PopoverTrigger asChild>
              <PermissionButton
                workspaceId={effectiveWorkspaceId}
                permission="create_folder"
                tooltipMessage="You don't have permission to create folders. Contact your team admin."
                onClick={() => {
                  setNewFolderParentId(folder.id);
                  setPopoverOpen(true);
                }}
              >
                <FolderPlus className="mr-2 h-4 w-4" /> New Folder
              </PermissionButton>
            </PopoverTrigger>
            <PopoverContent className="w-80">
              <div className="space-y-4">
                <div>
                  <h4 className="font-medium">Create New Folder</h4>
                  <p className="text-sm text-muted-foreground">
                    Enter a name for the new folder in "
                    {nodes[newFolderParentId]?.name || "Unknown"}".
                  </p>
                </div>
                <div className="space-y-2">
                  <Input
                    id="new-folder-name"
                    name="newFolderName"
                    placeholder="Folder name"
                    value={newFolderName}
                    onChange={(e) => {
                      setNewFolderName(e.target.value);
                      if (createFolderError) {
                        setCreateFolderError(null);
                      }
                    }}
                    onKeyDown={(e) => {
                      if (e.key === "Enter") {
                        handleCreateFolder();
                      }
                    }}
                  />
                  {createFolderError && (
                    <p className="text-sm text-red-500">{createFolderError}</p>
                  )}
                </div>
                <div className="flex justify-end gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setPopoverOpen(false);
                      setNewFolderName("");
                      setCreateFolderError(null);
                      setNewFolderParentId(folder.id);
                    }}
                  >
                    Cancel
                  </Button>
                  <Button
                    size="sm"
                    onClick={handleCreateFolder}
                    disabled={!newFolderName.trim() || isCreatingFolder}
                  >
                    {isCreatingFolder ? "Creating..." : "Create"}
                  </Button>
                </div>
              </div>
            </PopoverContent>
          </Popover>
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <span>
                  <PermissionButton
                    workspaceId={effectiveWorkspaceId}
                    permission="create_app"
                    tooltipMessage="You don't have permission to create apps. Contact your team admin."
                    variant="secondary"
                    onClick={handleShowCreateAppForm}
                    disabled={
                      folder.id === "root" ||
                      !(hasResources || loadingResources)
                    }
                  >
                    <FilePlus2 className="mr-2 h-4 w-4" />
                    {showCreateAppForm ? "Cancel" : "New App"}
                  </PermissionButton>
                </span>
              </TooltipTrigger>
              {(folder.id === "root" ||
                !(hasResources || loadingResources)) && (
                <TooltipContent>
                  <p>
                    {folder.id === "root"
                      ? "Apps must be created inside a folder. Please create or select a folder first."
                      : "Create a resource first before creating an app."}
                  </p>
                </TooltipContent>
              )}
            </Tooltip>
          </TooltipProvider>
        </div>
      </header>

      {showCreateAppForm && (
        <Card>
          <CardHeader>
            <CardTitle>Create New App</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {createAppError && (
              <div className="text-red-500 text-sm bg-red-50 p-3 rounded">
                {createAppError}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="app-name">App Name *</Label>
              <Input
                id="app-name"
                placeholder="Enter app name"
                value={appFormData.name}
                onChange={(e) => {
                  setAppFormData({ ...appFormData, name: e.target.value });
                  if (createAppError) {
                    setCreateAppError(null);
                  }
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    handleCreateApp();
                  }
                }}
                disabled={isCreatingApp}
              />
            </div>

            <AppConfigForm
              resourceId={appFormData.resourceId}
              urlPath={appFormData.urlPath}
              queryParameters={appFormData.queryParameters}
              headers={appFormData.headers}
              body={appFormData.body}
              onResourceChange={(resourceId) =>
                setAppFormData({ ...appFormData, resourceId })
              }
              onUrlPathChange={(urlPath) =>
                setAppFormData({ ...appFormData, urlPath })
              }
              onQueryParametersChange={(queryParameters) =>
                setAppFormData({ ...appFormData, queryParameters })
              }
              onHeadersChange={(headers) =>
                setAppFormData({ ...appFormData, headers })
              }
              onBodyChange={(body) => setAppFormData({ ...appFormData, body })}
              disabled={isCreatingApp}
            />

            <div className="flex gap-2 pt-2">
              <Button
                onClick={handleCreateApp}
                disabled={isCreatingApp || !appFormData.name.trim()}
              >
                {isCreatingApp ? "Creating..." : "Create App"}
              </Button>
              <Button
                onClick={handleCancelCreateApp}
                variant="outline"
                disabled={isCreatingApp}
              >
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <Separator />
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {children.map((child) => (
          <Card
            key={child.id}
            className="transition-transform hover:scale-[1.01] cursor-pointer"
            onClick={() => handleCardClick(child)}
          >
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base font-medium">
                {child.name}
              </CardTitle>
              <div className="flex items-center gap-2">
                {child.type === "folder" ? (
                  <Popover
                    open={cardPopoverOpen && newFolderParentId === child.id}
                    onOpenChange={(open) => {
                      if (!open) {
                        setCardPopoverOpen(false);
                        setNewFolderName("");
                        setCreateFolderError(null);
                        setNewFolderParentId(folder.id);
                      }
                    }}
                  >
                    <PopoverTrigger asChild>
                      <PermissionButton
                        workspaceId={effectiveWorkspaceId}
                        permission="create_folder"
                        tooltipMessage="You don't have permission to create folders. Contact your team admin."
                        variant="secondary"
                        size="icon"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleFolderCardPlusClick(child.id);
                        }}
                        aria-label="New Folder"
                      >
                        <Plus size={16} />
                      </PermissionButton>
                    </PopoverTrigger>
                    <PopoverContent className="w-80">
                      <div className="space-y-4">
                        <div>
                          <h4 className="font-medium">Create New Folder</h4>
                          <p className="text-sm text-muted-foreground">
                            Enter a name for the new folder in "
                            {nodes[newFolderParentId]?.name || "Unknown"}".
                          </p>
                        </div>
                        <div className="space-y-2">
                          <Input
                            id={`nested-folder-name-${newFolderParentId}`}
                            name="nestedFolderName"
                            placeholder="Folder name"
                            value={newFolderName}
                            onChange={(e) => {
                              setNewFolderName(e.target.value);
                              if (createFolderError) {
                                setCreateFolderError(null);
                              }
                            }}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") {
                                handleCreateFolder();
                              }
                            }}
                          />
                          {createFolderError && (
                            <p className="text-sm text-red-500">
                              {createFolderError}
                            </p>
                          )}
                        </div>
                        <div className="flex justify-end gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => {
                              setCardPopoverOpen(false);
                              setNewFolderName("");
                              setCreateFolderError(null);
                              setNewFolderParentId(folder.id);
                            }}
                          >
                            Cancel
                          </Button>
                          <Button
                            size="sm"
                            onClick={handleCreateFolder}
                            disabled={!newFolderName.trim() || isCreatingFolder}
                          >
                            {isCreatingFolder ? "Creating..." : "Create"}
                          </Button>
                        </div>
                      </div>
                    </PopoverContent>
                  </Popover>
                ) : (
                  <>
                    <Button
                      variant="secondary"
                      size="icon"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleRunApp(child.id);
                      }}
                      aria-label="Run App"
                    >
                      <Play size={16} />
                    </Button>
                    <PermissionButton
                      workspaceId={effectiveWorkspaceId}
                      permission="edit_app"
                      tooltipMessage="You don't have permission to edit apps. Contact your team admin."
                      variant="secondary"
                      size="icon"
                      onClick={(e) => {
                        e.stopPropagation();
                        onSelect(child.id);
                      }}
                      aria-label="Edit"
                      hideWhenDisabled
                    >
                      <Edit size={16} />
                    </PermissionButton>
                  </>
                )}
                <PermissionButton
                  workspaceId={effectiveWorkspaceId}
                  permission={
                    child.type === "folder" ? "delete_folder" : "delete_app"
                  }
                  tooltipMessage={
                    child.type === "folder"
                      ? "You don't have permission to delete folders. Contact your team admin."
                      : "You don't have permission to delete apps. Contact your team admin."
                  }
                  variant="secondary"
                  size="icon"
                  onClick={async (e) => {
                    e.stopPropagation();
                    if (child.type === "folder") {
                      handleDeleteClick(child.id);
                    } else {
                      // Delete app using API
                      try {
                        const response = await deleteApp(child.id);
                        if (!response.error) {
                          // Remove from local state on successful API call
                          deleteNode(child.id);
                        }
                      } catch {
                        // Silently ignore delete errors
                      }
                    }
                  }}
                  aria-label="Delete"
                  hideWhenDisabled
                >
                  <Trash2 size={16} />
                </PermissionButton>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                {child.type === "folder" ? "Folder" : "App"}
              </p>
            </CardContent>
          </Card>
        ))}
        {children.length === 0 && (
          <Card>
            <CardContent className="py-10 text-center text-muted-foreground">
              Empty folder. Create your first item.
            </CardContent>
          </Card>
        )}
      </div>

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={deleteConfirmOpen}
        onOpenChange={(open) => {
          if (!open) {
            handleDeleteCancel();
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="text-red-600">
              Delete Folder
            </AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. This will permanently delete the
              folder and all its contents.
            </AlertDialogDescription>
          </AlertDialogHeader>

          {deletingChildId && (
            <div className="space-y-3">
              <p className="text-sm">
                Type{" "}
                <strong className="font-semibold">
                  {nodes[deletingChildId]?.name}
                </strong>{" "}
                to confirm deletion:
              </p>
              <Input
                placeholder="Enter folder name"
                value={deleteConfirmName}
                onChange={(e) => {
                  setDeleteConfirmName(e.target.value);
                  if (deleteError) {
                    setDeleteError(null);
                  }
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    handleDeleteConfirm();
                  }
                }}
                autoFocus
              />
              {deleteError && (
                <p className="text-sm text-red-500">{deleteError}</p>
              )}
            </div>
          )}

          <AlertDialogFooter>
            <Button
              variant="outline"
              onClick={handleDeleteCancel}
              disabled={isDeleting}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteConfirm}
              disabled={
                !(deletingChildId && deleteConfirmName) ||
                deleteConfirmName !== nodes[deletingChildId]?.name ||
                isDeleting
              }
            >
              {isDeleting ? "Deleting..." : "Delete"}
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  );
}

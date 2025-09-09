import { useMemo, useState, useEffect } from "react";
import { Plus, FolderPlus, FilePlus2, Edit, Trash2, Play } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { FolderNode, WorkspaceMainProps, KeyValuePair } from "../types";
import {
  createFolder as createFolderAPI,
  deleteFolder as deleteFolderAPI,
  deleteApp,
  updateFolderName,
  runApp,
} from "@/api/api";
import AppConfigForm from "./AppConfigForm";

export default function FolderView({
  folder,
  nodes,
  onSelect,
  createApp,
  updateNode,
  deleteNode,
}: WorkspaceMainProps & { folder: FolderNode }) {
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(folder.name);

  // Update name when folder changes
  useEffect(() => {
    setName(folder.name);
    setEditing(false);
    setRenameError(null);
  }, [folder.id, folder.name]);

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
  }, [folder.id]);
  const [newFolderName, setNewFolderName] = useState("");
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);
  const [popoverOpen, setPopoverOpen] = useState(false);
  const [cardPopoverOpen, setCardPopoverOpen] = useState(false);
  const [newFolderParentId, setNewFolderParentId] = useState<string>(folder.id);
  const [createFolderError, setCreateFolderError] = useState<string | null>(
    null,
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
  const [runningAppId, setRunningAppId] = useState<string | null>(null);
  const [runResult, setRunResult] = useState<any>(null);
  const [runError, setRunError] = useState<string | null>(null);

  const children = useMemo(
    () => folder.childrenIds.map((id) => nodes[id]).filter(Boolean),
    [folder.childrenIds, nodes],
  );

  const handleCreateFolder = async () => {
    if (!newFolderName.trim()) return;

    setIsCreatingFolder(true);
    setCreateFolderError(null);
    try {
      const response = await createFolderAPI(newFolderName, newFolderParentId);
      if (response.error) {
        setCreateFolderError(
          (response.error?.message as string) || "Failed to create folder",
        );
      } else if (response.data) {
        // Add the new folder to the local state using the API response
        const newFolder = response.data;
        const folderNode = {
          id: newFolder.id!,
          name: newFolder.name,
          type: "folder" as const,
          parentId: newFolderParentId,
          childrenIds: [],
        };

        // Update nodes and parent's childrenIds
        updateNode(folderNode);
        const parentNode = nodes[newFolderParentId];
        if (parentNode && parentNode.type === "folder") {
          const updatedParent = {
            ...parentNode,
            childrenIds: [...parentNode.childrenIds, newFolder.id!],
          };
          updateNode(updatedParent);
        }

        // Reset form
        setNewFolderName("");
        setPopoverOpen(false);
        setCardPopoverOpen(false);
        setNewFolderParentId(folder.id);
      }
    } catch (error) {
      setCreateFolderError(
        error instanceof Error ? error.message : "Failed to create folder",
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
    if (!deletingChildId) return;

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
          (response.error?.message as string) || "Failed to delete folder",
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
        error instanceof Error ? error.message : "Failed to delete folder",
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
          (response.error?.message as string) || "Failed to rename folder",
        );
      } else {
        // Update local state
        updateNode({ ...folder, name: name.trim() });
        setEditing(false);
      }
    } catch (error) {
      setRenameError(
        error instanceof Error ? error.message : "Failed to rename folder",
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
        appFormData.body,
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
    navigate(`/workspaces/${appId}/run`);
  };

  const handleCardClick = (child: any) => {
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
                    if (renameError) setRenameError(null);
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
            <Button
              variant="secondary"
              size="icon"
              onClick={() => setEditing((v) => !v)}
              aria-label="Rename folder"
            >
              <Edit size={16} />
            </Button>
          )}
        </div>
        <div className="flex gap-2">
          <Popover open={popoverOpen} onOpenChange={setPopoverOpen}>
            <PopoverTrigger asChild>
              <Button
                onClick={() => {
                  setNewFolderParentId(folder.id);
                  setPopoverOpen(true);
                }}
              >
                <FolderPlus className="mr-2 h-4 w-4" /> New Folder
              </Button>
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
                    placeholder="Folder name"
                    value={newFolderName}
                    onChange={(e) => {
                      setNewFolderName(e.target.value);
                      if (createFolderError) setCreateFolderError(null);
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
                  <Button
                    variant="secondary"
                    onClick={handleShowCreateAppForm}
                    disabled={folder.id === "root"}
                  >
                    <FilePlus2 className="mr-2 h-4 w-4" />
                    {showCreateAppForm ? "Cancel" : "New App"}
                  </Button>
                </span>
              </TooltipTrigger>
              {folder.id === "root" && (
                <TooltipContent>
                  <p>
                    Apps must be created inside a folder. Please create or
                    select a folder first.
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
                  if (createAppError) setCreateAppError(null);
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
                      <Button
                        variant="secondary"
                        size="icon"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleFolderCardPlusClick(child.id);
                        }}
                        aria-label="New Folder"
                      >
                        <Plus size={16} />
                      </Button>
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
                            placeholder="Folder name"
                            value={newFolderName}
                            onChange={(e) => {
                              setNewFolderName(e.target.value);
                              if (createFolderError) setCreateFolderError(null);
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
                    <Button
                      variant="secondary"
                      size="icon"
                      onClick={(e) => {
                        e.stopPropagation();
                        onSelect(child.id);
                      }}
                      aria-label="Edit"
                    >
                      <Edit size={16} />
                    </Button>
                  </>
                )}
                <Button
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
                        if (response.error) {
                          console.error(
                            "Failed to delete app:",
                            response.error,
                          );
                        } else {
                          // Remove from local state on successful API call
                          deleteNode(child.id);
                        }
                      } catch (error) {
                        console.error("Error deleting app:", error);
                      }
                    }
                  }}
                  aria-label="Delete"
                >
                  <Trash2 size={16} />
                </Button>
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
                  if (deleteError) setDeleteError(null);
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
                !deletingChildId ||
                !deleteConfirmName ||
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

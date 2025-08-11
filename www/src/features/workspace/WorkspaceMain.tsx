import { useMemo, useState, useEffect } from "react";
import { Plus, FolderPlus, FilePlus2, Edit, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
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
  WorkspaceNode,
  FolderNode,
  AppNode,
  AppField,
  FieldType,
  Endpoint,
} from "./types";
import AppFormRenderer from "./components/AppFormRenderer";
import ResourcesView from "./components/ResourcesView";
import AuditLogView from "./components/AuditLogView";
import UsersTeamsView from "./components/UsersTeamsView";
import HttpMethodBadge from "./components/HttpMethodBadge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import {
  createFolder as createFolderAPI,
  deleteFolder as deleteFolderAPI,
  updateFolderName,
} from "@/api/api";

interface WorkspaceMainProps {
  nodes: Record<string, WorkspaceNode>;
  selectedId: string;
  onSelect: (id: string) => void;
  updateNode: (node: WorkspaceNode) => void;
  createFolder: (parentId: string) => void;
  createApp: (parentId: string) => void;
  deleteNode: (id: string) => void;
  endpoints: Record<string, Endpoint>;
  createEndpoint: () => void;
  updateEndpoint: (ep: Endpoint) => void;
  deleteEndpoint: (id: string) => void;
}

export default function WorkspaceMain(props: WorkspaceMainProps) {
  const { nodes, selectedId } = props;
  const selected = nodes[selectedId];

  // Handle special sections from sidebar
  if (selectedId === "resources") {
    return <ResourcesView />;
  }

  if (selectedId === "audit-log") {
    return <AuditLogView />;
  }

  if (selectedId === "users-&-teams") {
    return <UsersTeamsView />;
  }

  if (!selected) return null;

  if (selected.type === "folder") {
    return <FolderView {...props} folder={selected} />;
  }

  return <AppView {...props} app={selected} />;
}

function FolderView({
  folder,
  nodes,
  onSelect,
  createFolder,
  createApp,
  updateNode,
  deleteNode,
}: WorkspaceMainProps & { folder: FolderNode }) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(folder.name);

  // Update name when folder changes
  useEffect(() => {
    setName(folder.name);
    setEditing(false);
    setRenameError(null);
  }, [folder.id, folder.name]);
  const [newFolderName, setNewFolderName] = useState("");
  const [isCreatingFolder, setIsCreatingFolder] = useState(false);
  const [popoverOpen, setPopoverOpen] = useState(false);
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

  const children = useMemo(
    () => folder.childrenIds.map((id) => nodes[id]).filter(Boolean),
    [folder.childrenIds, nodes],
  );

  const handleCreateFolder = async () => {
    if (!newFolderName.trim()) return;

    setIsCreatingFolder(true);
    setCreateFolderError(null);
    try {
      const response = await createFolderAPI(newFolderName, folder.id);
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
          parentId: folder.id,
          childrenIds: [],
        };

        // Update nodes and parent's childrenIds
        updateNode(folderNode);
        const updatedParent = {
          ...folder,
          childrenIds: [...folder.childrenIds, newFolder.id!],
        };
        updateNode(updatedParent);

        // Reset form
        setNewFolderName("");
        setPopoverOpen(false);
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
              <Button>
                <FolderPlus className="mr-2 h-4 w-4" /> New Folder
              </Button>
            </PopoverTrigger>
            <PopoverContent className="w-80">
              <div className="space-y-4">
                <div>
                  <h4 className="font-medium">Create New Folder</h4>
                  <p className="text-sm text-muted-foreground">
                    Enter a name for the new folder.
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
          <Button variant="secondary" onClick={() => createApp(folder.id)}>
            <FilePlus2 className="mr-2 h-4 w-4" /> New App
          </Button>
        </div>
      </header>
      <Separator />
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {children.map((child) => (
          <Card
            key={child.id}
            className="transition-transform hover:scale-[1.01] cursor-pointer"
            onClick={() => onSelect(child.id)}
          >
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base font-medium">
                {child.name}
              </CardTitle>
              <div className="flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="icon"
                  onClick={(e) => {
                    e.stopPropagation();
                    onSelect(child.id);
                  }}
                  aria-label={child.type === "folder" ? "Open" : "Edit"}
                >
                  {child.type === "folder" ? (
                    <Plus size={16} />
                  ) : (
                    <Edit size={16} />
                  )}
                </Button>
                <Button
                  variant="secondary"
                  size="icon"
                  onClick={(e) => {
                    e.stopPropagation();
                    child.type === "folder"
                      ? handleDeleteClick(child.id)
                      : deleteNode(child.id);
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

function AppView({
  app,
  updateNode,
  endpoints,
}: WorkspaceMainProps & { app: AppNode }) {
  const [tab, setTab] = useState("build");
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(app.name);

  // Update name when app changes
  useEffect(() => {
    setName(app.name);
    setEditing(false);
  }, [app.id, app.name]);

  const updateField = (id: string, patch: Partial<AppField>) => {
    updateNode({
      ...app,
      fields: app.fields.map((f) => (f.id === id ? { ...f, ...patch } : f)),
    });
  };
  const deleteField = (id: string) => {
    updateNode({ ...app, fields: app.fields.filter((f) => f.id !== id) });
  };
  const addField = () => {
    const id = crypto.randomUUID();
    const nf: AppField = {
      id,
      label: `Field ${app.fields.length + 1}`,
      type: "text",
    };
    updateNode({ ...app, fields: [...app.fields, nf] });
  };

  const endpointList = Object.values(endpoints);
  const selectedEndpoint = app.endpointId
    ? endpoints[app.endpointId]
    : undefined;

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          {editing ? (
            <div className="flex items-center gap-2">
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-64"
              />
              <Button
                onClick={() => {
                  updateNode({ ...app, name });
                  setEditing(false);
                }}
              >
                Save
              </Button>
            </div>
          ) : (
            <h2 className="text-2xl font-semibold">{app.name}</h2>
          )}
          <Button
            variant="secondary"
            size="icon"
            onClick={() => setEditing((v) => !v)}
            aria-label="Rename app"
          >
            <Edit size={16} />
          </Button>
        </div>
        <div className="flex gap-2 items-center">
          <div className="w-64">
            <Select
              value={app.endpointId || ""}
              onValueChange={(v) =>
                updateNode({ ...app, endpointId: v || undefined })
              }
            >
              <SelectTrigger aria-label="Select Endpoint">
                <SelectValue placeholder="Select Endpoint" />
              </SelectTrigger>
              <SelectContent>
                {endpointList.map((ep) => (
                  <SelectItem key={ep.id} value={ep.id}>
                    <div className="flex items-center gap-2">
                      <span>{ep.name}</span>
                      <HttpMethodBadge method={ep.method} />
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <Button onClick={addField}>
            <Plus className="mr-2 h-4 w-4" /> Add Field
          </Button>
        </div>
      </header>
      <Separator />
      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="build">Build</TabsTrigger>
          <TabsTrigger value="fill">Fill</TabsTrigger>
        </TabsList>
        <TabsContent value="build" className="space-y-6">
          {app.fields.length === 0 && (
            <Card>
              <CardContent className="py-10 text-center text-muted-foreground">
                No fields. Add one to start.
              </CardContent>
            </Card>
          )}
          {app.fields.map((f) => (
            <Card key={f.id}>
              <CardContent className="py-4 grid grid-cols-1 md:grid-cols-12 gap-3 items-center">
                <div className="md:col-span-4">
                  <Input
                    value={f.label}
                    onChange={(e) =>
                      updateField(f.id, { label: e.target.value })
                    }
                    aria-label="Field label"
                  />
                </div>
                <div className="md:col-span-3">
                  <Select
                    value={f.type}
                    onValueChange={(v: FieldType) =>
                      updateField(f.id, { type: v })
                    }
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Type" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="text">Text</SelectItem>
                      <SelectItem value="email">Email</SelectItem>
                      <SelectItem value="date">Date</SelectItem>
                      <SelectItem value="integer">Integer</SelectItem>
                      <SelectItem value="boolean">Boolean</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="md:col-span-3 flex items-center gap-2">
                  <Switch
                    checked={!!f.required}
                    onCheckedChange={(v) => updateField(f.id, { required: v })}
                  />
                  <span className="text-sm text-muted-foreground">
                    Required
                  </span>
                </div>
                <div className="md:col-span-2 flex justify-end">
                  <Button
                    variant="secondary"
                    size="icon"
                    onClick={() => deleteField(f.id)}
                    aria-label="Delete field"
                  >
                    <Trash2 size={16} />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </TabsContent>
        <TabsContent value="fill">
          <AppFormRenderer app={app} endpoint={selectedEndpoint} />
        </TabsContent>
      </Tabs>
    </section>
  );
}

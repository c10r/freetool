import { useMemo, useState, useEffect } from "react";
import { Plus, FolderPlus, FilePlus2, Edit, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Separator } from "@/components/ui/separator";
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
import HttpMethodBadge from "./components/HttpMethodBadge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";

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
    return (
      <ResourcesView
        endpoints={props.endpoints}
        createEndpoint={props.createEndpoint}
        updateEndpoint={props.updateEndpoint}
        deleteEndpoint={props.deleteEndpoint}
      />
    );
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

  const children = useMemo(
    () => folder.childrenIds.map((id) => nodes[id]).filter(Boolean),
    [folder.childrenIds, nodes],
  );

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
                  updateNode({ ...folder, name });
                  setEditing(false);
                }}
              >
                Save
              </Button>
            </div>
          ) : (
            <h2 className="text-2xl font-semibold">{folder.name}</h2>
          )}
          <Button
            variant="secondary"
            size="icon"
            onClick={() => setEditing((v) => !v)}
            aria-label="Rename folder"
          >
            <Edit size={16} />
          </Button>
        </div>
        <div className="flex gap-2">
          <Button onClick={() => createFolder(folder.id)}>
            <FolderPlus className="mr-2 h-4 w-4" /> New Folder
          </Button>
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
            className="transition-transform hover:scale-[1.01]"
          >
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base font-medium">
                {child.name}
              </CardTitle>
              <div className="flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="icon"
                  onClick={() => onSelect(child.id)}
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
                  onClick={() => deleteNode(child.id)}
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

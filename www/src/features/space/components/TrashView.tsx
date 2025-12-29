import { Database, FileType, Folder, RotateCcw, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import {
  getTrashBySpace,
  restoreApp,
  restoreFolder,
  restoreResource,
} from "@/api/api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

interface TrashItem {
  id: string;
  name: string;
  itemType: string;
  spaceId: string;
  deletedAt: string;
}

interface RestoreResult {
  newName?: string;
  autoRestoredResourceId?: string;
}

interface TrashViewProps {
  spaceId: string;
  spaceName: string;
  onRestoreComplete?: () => void;
}

export default function TrashView({
  spaceId,
  spaceName,
  onRestoreComplete,
}: TrashViewProps) {
  const [items, setItems] = useState<TrashItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [restoring, setRestoring] = useState<string | null>(null);

  const fetchTrash = useCallback(async () => {
    setLoading(true);
    try {
      const response = await getTrashBySpace(spaceId);
      if (response.data) {
        const trashItems = response.data.items || [];
        setItems(
          trashItems.map((item) => ({
            id: item.id ?? "",
            name: item.name ?? "",
            itemType: item.itemType ?? "",
            spaceId: item.spaceId ?? "",
            deletedAt: item.deletedAt ?? "",
          }))
        );
      }
    } catch (_error) {
      toast.error("Failed to load trash");
    } finally {
      setLoading(false);
    }
  }, [spaceId]);

  useEffect(() => {
    fetchTrash();
  }, [fetchTrash]);

  const performRestore = async (
    item: TrashItem
  ): Promise<RestoreResult | null> => {
    switch (item.itemType) {
      case "app": {
        const response = await restoreApp(item.id);
        return response.data as RestoreResult | null;
      }
      case "folder": {
        const response = await restoreFolder(item.id);
        return response.data as RestoreResult | null;
      }
      case "resource": {
        const response = await restoreResource(item.id);
        return response.data as RestoreResult | null;
      }
      default:
        throw new Error(`Unknown item type: ${item.itemType}`);
    }
  };

  const handleRestore = async (item: TrashItem) => {
    setRestoring(item.id);
    try {
      const result = await performRestore(item);

      if (result) {
        let message = `${item.itemType.charAt(0).toUpperCase() + item.itemType.slice(1)} "${item.name}" restored`;

        if (result.newName) {
          message += ` as "${result.newName}"`;
        }
        if (result.autoRestoredResourceId) {
          message += " (resource was also restored)";
        }

        toast.success(message);
        await fetchTrash();
        onRestoreComplete?.();
      }
    } catch (_error) {
      toast.error(`Failed to restore ${item.itemType}`);
    } finally {
      setRestoring(null);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const getItemIcon = (itemType: string) => {
    switch (itemType) {
      case "folder":
        return <Folder className="h-4 w-4" />;
      case "app":
        return <FileType className="h-4 w-4" />;
      case "resource":
        return <Database className="h-4 w-4" />;
      default:
        return null;
    }
  };

  const getItemBadgeVariant = (
    itemType: string
  ): "secondary" | "default" | "outline" => {
    switch (itemType) {
      case "folder":
        return "secondary";
      case "app":
        return "default";
      case "resource":
        return "outline";
      default:
        return "secondary";
    }
  };

  if (loading) {
    return (
      <div className="p-6">
        <div className="flex items-center gap-3 mb-6">
          <Trash2 className="h-6 w-6" />
          <h1 className="text-2xl font-bold">Trash - {spaceName}</h1>
        </div>
        <div className="flex items-center justify-center py-12">
          <span className="text-muted-foreground">Loading...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="flex items-center gap-3 mb-6">
        <Trash2 className="h-6 w-6" />
        <h1 className="text-2xl font-bold">Trash - {spaceName}</h1>
      </div>

      <p className="text-muted-foreground mb-6">
        Items in trash can be restored to their original location. Restoring a
        folder will also restore all its contents.
      </p>

      {items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <Trash2 className="h-12 w-12 text-muted-foreground/50 mb-4" />
          <h3 className="text-lg font-medium">Trash is empty</h3>
          <p className="text-muted-foreground">
            Deleted apps, folders, and resources will appear here.
          </p>
        </div>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Deleted</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell className="font-medium">
                    <div className="flex items-center gap-2">
                      {getItemIcon(item.itemType)}
                      {item.name}
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant={getItemBadgeVariant(item.itemType)}>
                      {item.itemType}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {formatDate(item.deletedAt)}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleRestore(item)}
                      disabled={restoring === item.id}
                    >
                      <RotateCcw className="h-4 w-4 mr-2" />
                      {restoring === item.id ? "Restoring..." : "Restore"}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}

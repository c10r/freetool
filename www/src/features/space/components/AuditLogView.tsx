import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import {
  getAppAuditEvents,
  getAuditEvents,
  getUserAuditEvents,
} from "@/api/api";
import { PaginationControls } from "@/components/PaginationControls";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { DEFAULT_PAGE_SIZE } from "@/lib/pagination";

// Safe JSON parsing helper
const safeJsonParse = (
  jsonString: string
): Record<string, unknown> | { error: string; raw: string } => {
  try {
    return JSON.parse(jsonString) as Record<string, unknown>;
  } catch (_error) {
    return { error: "Invalid JSON data", raw: jsonString };
  }
};

type UserEvents = "UserCreatedEvent" | "UserUpdatedEvent" | "UserDeletedEvent";

type AppEvents =
  | "AppCreatedEvent"
  | "AppUpdatedEvent"
  | "AppDeletedEvent"
  | "AppRestoredEvent";

type FolderEvents =
  | "FolderCreatedEvent"
  | "FolderUpdatedEvent"
  | "FolderDeletedEvent"
  | "FolderRestoredEvent";

type SpaceEvents =
  | "SpaceCreatedEvent"
  | "SpaceUpdatedEvent"
  | "SpaceDeletedEvent"
  | "SpacePermissionsChangedEvent";

type ResourceEvents =
  | "ResourceCreatedEvent"
  | "ResourceUpdatedEvent"
  | "ResourceDeletedEvent"
  | "ResourceRestoredEvent";

type RunEvents = "RunCreatedEvent" | "RunStatusChangedEvent";

type EventType =
  | UserEvents
  | AppEvents
  | ResourceEvents
  | FolderEvents
  | SpaceEvents
  | RunEvents;

type EntityType = "User" | "Folder" | "Space" | "App" | "Resource" | "Run";

interface AuditEvent {
  id: string;
  eventId: string;
  eventType: EventType;
  entityType: EntityType;
  entityId: string;
  entityName: string;
  userId: string;
  userName: string;
  eventSummary: string;
  occurredAt: string;
  createdAt: string;
  eventData: string;
}

type AuditScope = "global" | "app" | "user";

const getEventTypeColor = (eventType: EventType): string => {
  if (eventType.includes("Created")) {
    return "bg-green-100 text-green-800";
  }
  if (eventType.includes("Updated")) {
    return "bg-blue-100 text-blue-800";
  }
  if (eventType.includes("Deleted")) {
    return "bg-red-100 text-red-800";
  }
  if (eventType.includes("Restored")) {
    return "bg-teal-100 text-teal-800";
  }
  if (eventType.includes("StatusChanged")) {
    return "bg-yellow-100 text-yellow-800";
  }
  if (eventType.includes("PermissionsChanged")) {
    return "bg-amber-100 text-amber-800";
  }
  return "bg-gray-100 text-gray-800";
};

const getEntityTypeColor = (entityType: EntityType): string => {
  switch (entityType) {
    case "User":
      return "bg-purple-100 text-purple-800";
    case "Folder":
      return "bg-orange-100 text-orange-800";
    case "Space":
      return "bg-indigo-100 text-indigo-800";
    case "App":
      return "bg-emerald-100 text-emerald-800";
    case "Resource":
      return "bg-cyan-100 text-cyan-800";
    case "Run":
      return "bg-pink-100 text-pink-800";
    default:
      return "bg-gray-100 text-gray-800";
  }
};

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleString();
};

export default function AuditLogView() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedEvent, setExpandedEvent] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);

  const pageSize = DEFAULT_PAGE_SIZE;
  const scope = useMemo<AuditScope>(() => {
    const scopeParam = searchParams.get("scope");
    if (scopeParam === "app" || scopeParam === "user") {
      return scopeParam;
    }
    return "global";
  }, [searchParams]);
  const scopedAppId = searchParams.get("appId");
  const scopedUserId = searchParams.get("userId");

  const currentPage = useMemo(() => {
    const pageParam = searchParams.get("page");
    const parsedPage = pageParam ? Number.parseInt(pageParam, 10) : 1;
    return Number.isNaN(parsedPage) || parsedPage < 1 ? 1 : parsedPage;
  }, [searchParams]);
  const skip = (currentPage - 1) * pageSize;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const goToPage = useCallback(
    (page: number) => {
      const validPage = Math.max(1, Math.min(page, totalPages));
      const nextSearchParams = new URLSearchParams(searchParams);

      if (validPage === 1) {
        nextSearchParams.delete("page");
      } else {
        nextSearchParams.set("page", String(validPage));
      }

      setSearchParams(nextSearchParams);
    },
    [searchParams, setSearchParams, totalPages]
  );

  const fetchAuditEvents = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response =
        scope === "app"
          ? scopedAppId
            ? await getAppAuditEvents(scopedAppId, skip, pageSize, true)
            : null
          : scope === "user"
            ? scopedUserId
              ? await getUserAuditEvents(scopedUserId, skip, pageSize)
              : null
            : await getAuditEvents(skip, pageSize);

      if (!response) {
        setEvents([]);
        setTotalCount(0);
        setError(
          scope === "app"
            ? "Missing app ID for app audit log"
            : "Missing user ID for user audit log"
        );
        return;
      }

      if (response.data?.items) {
        const mappedItems = response.data?.items?.map((item) => {
          return {
            id: item.id,
            eventId: item.eventId,
            eventType: item.eventType,
            entityType: item.entityType,
            entityId: item.entityId,
            entityName: item.entityName,
            userId: item.userId,
            userName: item.userName,
            eventSummary: item.eventSummary,
            occurredAt: item.occurredAt,
            createdAt: item.createdAt,
            eventData: item.eventData,
          };
        });
        setEvents(mappedItems);
        if (response.data.totalCount !== undefined) {
          setTotalCount(response.data.totalCount);
        }
      }
    } catch (_err) {
      setError("Failed to load audit events");
    } finally {
      setLoading(false);
    }
  }, [skip, pageSize, scope, scopedAppId, scopedUserId]);

  useEffect(() => {
    fetchAuditEvents();
  }, [fetchAuditEvents]);

  useEffect(() => {
    if (!loading && currentPage > totalPages) {
      goToPage(totalPages);
    }
  }, [currentPage, goToPage, loading, totalPages]);

  const toggleEventDetails = (eventId: string) => {
    setExpandedEvent(expandedEvent === eventId ? null : eventId);
  };

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h2 className="text-2xl font-semibold">
            {scope === "app"
              ? "App Audit Log"
              : scope === "user"
                ? "User Audit Log"
                : "Audit Log"}
          </h2>
          {totalCount > 0 && (
            <Badge variant="secondary">{totalCount} events</Badge>
          )}
        </div>
        <Button onClick={fetchAuditEvents}>Refresh</Button>
      </header>
      <Separator />

      {loading && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            Loading audit events...
          </CardContent>
        </Card>
      )}

      {error && (
        <Card>
          <CardContent className="py-10 text-center text-red-500">
            {error}
          </CardContent>
        </Card>
      )}

      {!(loading || error) && events.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No audit events found.
          </CardContent>
        </Card>
      )}

      {!(loading || error) && events.length > 0 && (
        <>
          <div className="space-y-2">
            {events.map((event) => (
              <Card
                key={event.id}
                className="transition-transform hover:scale-[1.005]"
              >
                <CardContent className="p-4">
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-3">
                      <Badge className={getEventTypeColor(event.eventType)}>
                        {event.eventType.replace("Event", "")}
                      </Badge>
                      <Badge className={getEntityTypeColor(event.entityType)}>
                        {event.entityType}
                      </Badge>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-muted-foreground">
                        {formatDate(event.occurredAt)}
                      </span>
                    </div>
                  </div>

                  <div className="mb-3">
                    <div className="text-sm font-medium">
                      {event.eventSummary}
                    </div>
                  </div>

                  <div className="flex justify-between items-center">
                    <div className="text-xs text-muted-foreground">
                      Event ID: {event.eventId.substring(0, 8)}...
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => toggleEventDetails(event.id)}
                    >
                      {expandedEvent === event.id
                        ? "Hide Details"
                        : "Show Details"}
                    </Button>
                  </div>

                  {expandedEvent === event.id && (
                    <div className="mt-4 pt-4 border-t space-y-4">
                      <div>
                        <h4 className="font-medium mb-2">Details:</h4>
                        <div className="text-sm text-muted-foreground space-y-1">
                          <div>
                            <strong>User:</strong> {event.userName} (
                            {event.userId})
                          </div>
                          <div>
                            <strong>Entity:</strong> {event.entityName} (
                            {event.entityId})
                          </div>
                          <div>
                            <strong>Event ID:</strong> {event.eventId}
                          </div>
                          <div>
                            <strong>Created At:</strong>{" "}
                            {formatDate(event.createdAt)}
                          </div>
                        </div>
                      </div>
                      <div>
                        <h4 className="font-medium mb-2">Raw Event Data:</h4>
                        <pre className="bg-gray-50 p-3 rounded text-xs overflow-x-auto">
                          {JSON.stringify(
                            safeJsonParse(event.eventData),
                            null,
                            2
                          )}
                        </pre>
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
          <PaginationControls
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={goToPage}
          />
        </>
      )}
    </section>
  );
}

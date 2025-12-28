import { useState, useEffect } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { getAuditEvents } from "@/api/api";

// Safe JSON parsing helper
const safeJsonParse = (jsonString: string): Record<string, unknown> | { error: string; raw: string } => {
  try {
    return JSON.parse(jsonString) as Record<string, unknown>;
  } catch (error) {
    console.warn("Failed to parse JSON:", error);
    return { error: "Invalid JSON data", raw: jsonString };
  }
};

type UserEvents = "UserCreatedEvent" | "UserUpdatedEvent" | "UserDeletedEvent";

type AppEvents = "AppCreatedEvent" | "AppUpdatedEvent" | "AppDeletedEvent";

type FolderEvents =
  | "FolderCreatedEvent"
  | "FolderUpdatedEvent"
  | "FolderDeletedEvent";

type GroupEvents =
  | "GroupCreatedEvent"
  | "GroupUpdatedEvent"
  | "GroupDeletedEvent";

type ResourceEvents =
  | "ResourceCreatedEvent"
  | "ResourceUpdatedEvent"
  | "ResourceDeletedEvent";

type RunEvents = "RunCreatedEvent" | "RunStatusChangedEvent";

type EventType =
  | UserEvents
  | AppEvents
  | ResourceEvents
  | FolderEvents
  | GroupEvents
  | RunEvents;

type EntityType = "User" | "Folder" | "Group" | "App" | "Resource" | "Run";

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

const getEventTypeColor = (eventType: EventType): string => {
  if (eventType.includes("Created")) return "bg-green-100 text-green-800";
  if (eventType.includes("Updated")) return "bg-blue-100 text-blue-800";
  if (eventType.includes("Deleted")) return "bg-red-100 text-red-800";
  if (eventType.includes("StatusChanged"))
    return "bg-yellow-100 text-yellow-800";
  return "bg-gray-100 text-gray-800";
};

const getEntityTypeColor = (entityType: EntityType): string => {
  switch (entityType) {
    case "User":
      return "bg-purple-100 text-purple-800";
    case "Folder":
      return "bg-orange-100 text-orange-800";
    case "Group":
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
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedEvent, setExpandedEvent] = useState<string | null>(null);

  const fetchAuditEvents = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getAuditEvents();
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
      }
    } catch (err) {
      setError("Failed to load audit events");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAuditEvents();
  }, []);

  const toggleEventDetails = (eventId: string) => {
    setExpandedEvent(expandedEvent === eventId ? null : eventId);
  };

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Audit Log</h2>
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

      {!loading && !error && events.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No audit events found.
          </CardContent>
        </Card>
      )}

      {!loading && !error && events.length > 0 && (
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
                          2,
                        )}
                      </pre>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </section>
  );
}

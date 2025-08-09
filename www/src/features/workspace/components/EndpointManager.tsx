import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Endpoint, EndpointMethod } from "../types";
import KeyValueList from "./KeyValueList";
import { Trash2, Plus } from "lucide-react";

interface EndpointManagerProps {
  endpoints: Record<string, Endpoint>;
  createEndpoint: () => void;
  updateEndpoint: (ep: Endpoint) => void;
  deleteEndpoint: (id: string) => void;
}

export default function EndpointManager({
  endpoints,
  createEndpoint,
  updateEndpoint,
  deleteEndpoint,
}: EndpointManagerProps) {
  const list = Object.values(endpoints);
  const methods: EndpointMethod[] = [
    "GET",
    "POST",
    "PUT",
    "PATCH",
    "DELETE",
    "HEAD",
    "OPTIONS",
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium">Endpoints</h3>
        <Button type="button" onClick={createEndpoint}>
          <Plus className="mr-2 h-4 w-4" /> New Endpoint
        </Button>
      </div>
      {list.length === 0 && (
        <Card>
          <CardContent className="py-8 text-center text-muted-foreground">
            No endpoints yet. Create one to start.
          </CardContent>
        </Card>
      )}
      {list.map((ep) => (
        <Card key={ep.id}>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base font-medium">{ep.name}</CardTitle>
            <Button
              variant="secondary"
              size="icon"
              onClick={() => deleteEndpoint(ep.id)}
              aria-label="Delete endpoint"
            >
              <Trash2 size={16} />
            </Button>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-12 gap-3 items-center">
              <div className="md:col-span-3">
                <Input
                  value={ep.name}
                  onChange={(e) =>
                    updateEndpoint({ ...ep, name: e.target.value })
                  }
                  aria-label="Endpoint name"
                />
              </div>
              <div className="md:col-span-2">
                <Select
                  value={ep.method}
                  onValueChange={(v: EndpointMethod) =>
                    updateEndpoint({ ...ep, method: v })
                  }
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Method" />
                  </SelectTrigger>
                  <SelectContent>
                    {methods.map((m) => (
                      <SelectItem key={m} value={m}>
                        {m}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="md:col-span-7">
                <Input
                  value={ep.url}
                  onChange={(e) =>
                    updateEndpoint({ ...ep, url: e.target.value })
                  }
                  placeholder="https://api.example.com/resource"
                  aria-label="Endpoint URL"
                />
              </div>
            </div>
            <Separator />
            <div className="space-y-2">
              <h4 className="text-sm font-medium">Headers</h4>
              <KeyValueList
                items={ep.headers}
                onChange={(items) => updateEndpoint({ ...ep, headers: items })}
                ariaLabel="Headers"
              />
            </div>
            <div className="space-y-2">
              <h4 className="text-sm font-medium">Query Parameters</h4>
              <KeyValueList
                items={ep.query}
                onChange={(items) => updateEndpoint({ ...ep, query: items })}
                ariaLabel="Query parameters"
              />
            </div>
            <div className="space-y-2">
              <h4 className="text-sm font-medium">JSON Body (static pairs)</h4>
              <KeyValueList
                items={ep.body}
                onChange={(items) => updateEndpoint({ ...ep, body: items })}
                ariaLabel="JSON body"
              />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

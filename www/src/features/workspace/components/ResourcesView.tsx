import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Endpoint } from "../types";
import EndpointManager from "./EndpointManager";

interface ResourcesViewProps {
  endpoints: Record<string, Endpoint>;
  createEndpoint: () => void;
  updateEndpoint: (ep: Endpoint) => void;
  deleteEndpoint: (id: string) => void;
}

export default function ResourcesView({
  endpoints,
  createEndpoint,
  updateEndpoint,
  deleteEndpoint,
}: ResourcesViewProps) {
  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Resources</h2>
      </header>
      <Separator />

      <Card>
        <CardContent>
          <EndpointManager
            endpoints={endpoints}
            createEndpoint={createEndpoint}
            updateEndpoint={updateEndpoint}
            deleteEndpoint={deleteEndpoint}
          />
        </CardContent>
      </Card>
    </section>
  );
}

import { cn } from "@/lib/utils";
import { EndpointMethod } from "../types";

interface HttpMethodBadgeProps {
  method: EndpointMethod;
  className?: string;
}

export default function HttpMethodBadge({
  method,
  className,
}: HttpMethodBadgeProps) {
  return (
    <span
      className={cn(
        "px-2 py-1 rounded text-xs font-medium",
        method === "GET" && "bg-green-100 text-green-800",
        method === "POST" && "bg-blue-100 text-blue-800",
        method === "PUT" && "bg-orange-100 text-orange-800",
        method === "PATCH" && "bg-yellow-100 text-yellow-800",
        method === "DELETE" && "bg-red-100 text-red-800",
        (method === "HEAD" || method === "OPTIONS") &&
          "bg-gray-100 text-gray-800",
        className,
      )}
    >
      {method}
    </span>
  );
}

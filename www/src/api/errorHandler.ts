import { toast } from "sonner";
import { getErrorMessage } from "./errorMessages";

/**
 * Display an error toast for API errors
 * @param status - HTTP status code
 * @param serverMessage - Optional message from the server response
 */
export function handleApiError(status: number, serverMessage?: string): void {
  const message = getErrorMessage(status, serverMessage);

  // Use longer duration for server errors, shorter for client errors
  const duration = status >= 500 ? 5000 : 4000;

  toast.error(message, {
    duration,
    // Use status-based ID to prevent duplicate toasts for the same error type
    id: `api-error-${status}`,
  });
}

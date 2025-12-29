// Centralized error messages for API errors
export const ERROR_MESSAGES: Record<number, string> = {
  401: "Your session has expired. Please refresh the page.",
  403: "You don't have permissions to do that - please ask your team admin",
  404: "The requested resource was not found",
  500: "Something went wrong, please ask your administrator",
};

// Default message for unhandled status codes
export const DEFAULT_ERROR_MESSAGE = "An unexpected error occurred";

// Get user-friendly error message based on status code
export function getErrorMessage(
  status: number,
  serverMessage?: string
): string {
  return ERROR_MESSAGES[status] || serverMessage || DEFAULT_ERROR_MESSAGE;
}

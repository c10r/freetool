/**
 * useAuthorization Hook
 *
 * Provides access to the authorization context.
 * Must be used within an AuthorizationProvider.
 */

import { useContext } from "react";
import {
  AuthorizationContext,
  type AuthorizationContextValue,
} from "@/contexts/authorization.context";

/**
 * Hook to access the authorization context
 *
 * @throws Error if used outside of AuthorizationProvider
 *
 * @example
 * ```tsx
 * function MyComponent() {
 *   const { currentUser, hasPermission } = useAuthorization();
 *
 *   if (hasPermission('workspace-1', 'create_app')) {
 *     // Show create button
 *   }
 * }
 * ```
 */
export function useAuthorization(): AuthorizationContextValue {
  const context = useContext(AuthorizationContext);
  if (!context) {
    throw new Error(
      "useAuthorization must be used within an AuthorizationProvider",
    );
  }
  return context;
}

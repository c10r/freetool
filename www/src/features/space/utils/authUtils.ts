import type { AuthConfig, KeyValuePair } from "../types";

const AUTHORIZATION_HEADER_KEY = "Authorization";
const REDACTED_PLACEHOLDER = "---- redacted ----";

/**
 * Check if a key is the Authorization header (case-insensitive)
 */
export function isAuthorizationHeader(key: string): boolean {
  return key.toLowerCase() === AUTHORIZATION_HEADER_KEY.toLowerCase();
}

/**
 * Parse Authorization header from headers array to AuthConfig
 */
export function parseAuthFromHeaders(headers: KeyValuePair[]): AuthConfig {
  const authHeader = headers.find((h) => isAuthorizationHeader(h.key));

  if (!authHeader?.value) {
    return { type: "none" };
  }

  const value = authHeader.value.trim();

  if (value.includes(REDACTED_PLACEHOLDER)) {
    if (value.toLowerCase().startsWith("basic ")) {
      return {
        type: "basic",
        username: REDACTED_PLACEHOLDER,
        password: REDACTED_PLACEHOLDER,
        isRedacted: true,
      };
    }

    return {
      type: "bearer",
      token: REDACTED_PLACEHOLDER,
      isRedacted: true,
    };
  }

  // Check for Bearer token
  if (value.startsWith("Bearer ")) {
    return {
      type: "bearer",
      token: value.substring(7), // Remove "Bearer " prefix
    };
  }

  // Check for Basic auth
  if (value.startsWith("Basic ")) {
    try {
      const decoded = atob(value.substring(6)); // Remove "Basic " prefix
      const colonIndex = decoded.indexOf(":");
      if (colonIndex !== -1) {
        return {
          type: "basic",
          username: decoded.substring(0, colonIndex),
          password: decoded.substring(colonIndex + 1),
        };
      }
    } catch {
      // Invalid base64, treat as no auth
    }
  }

  return { type: "none" };
}

/**
 * Convert AuthConfig to Authorization header value
 */
export function authConfigToHeaderValue(config: AuthConfig): string | null {
  switch (config.type) {
    case "none":
      return null;
    case "bearer":
      if (!config.token) {
        return null;
      }

      if (config.isRedacted) {
        return `Bearer ${REDACTED_PLACEHOLDER}`;
      }

      return `Bearer ${config.token}`;
    case "basic":
      if (config.isRedacted) {
        return `Basic ${REDACTED_PLACEHOLDER}`;
      }

      if (config.username || config.password) {
        return `Basic ${btoa(`${config.username}:${config.password}`)}`;
      }
      return null;
  }
}

/**
 * Remove Authorization header from headers array
 */
export function removeAuthFromHeaders(headers: KeyValuePair[]): KeyValuePair[] {
  return headers.filter((h) => !isAuthorizationHeader(h.key));
}

/**
 * Inject auth into headers array (replaces existing Authorization header or adds new one)
 */
export function injectAuthIntoHeaders(
  headers: KeyValuePair[],
  authConfig: AuthConfig
): KeyValuePair[] {
  // Remove any existing Authorization header
  const filteredHeaders = removeAuthFromHeaders(headers);

  // Get the new header value
  const headerValue = authConfigToHeaderValue(authConfig);

  // If no auth configured, return filtered headers
  if (!headerValue) {
    return filteredHeaders;
  }

  // Add the Authorization header
  return [
    ...filteredHeaders,
    { key: AUTHORIZATION_HEADER_KEY, value: headerValue },
  ];
}

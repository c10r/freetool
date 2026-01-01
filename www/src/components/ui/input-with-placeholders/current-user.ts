import type { AppInput } from "../input-with-placeholders.types";

export const CURRENT_USER_PREFIX = "current_user";

export const CURRENT_USER_PROPERTIES = [
  { key: "email", displayName: "Email", type: "string" },
  { key: "id", displayName: "ID", type: "string" },
  { key: "firstName", displayName: "First Name", type: "string" },
  { key: "lastName", displayName: "Last Name", type: "string" },
] as const;

export type CurrentUserProperty = (typeof CURRENT_USER_PROPERTIES)[number];

/**
 * Check if a placeholder title refers to a current_user property
 * e.g., "current_user.email" returns true, "myField" returns false
 */
export function isCurrentUserPlaceholder(title: string): boolean {
  return title.startsWith(`${CURRENT_USER_PREFIX}.`);
}

/**
 * Check if a current_user placeholder is valid (property exists)
 * e.g., "current_user.email" returns true, "current_user.invalid" returns false
 */
export function isValidCurrentUserPlaceholder(title: string): boolean {
  if (!isCurrentUserPlaceholder(title)) {
    return false;
  }
  const property = title.slice(CURRENT_USER_PREFIX.length + 1);
  return CURRENT_USER_PROPERTIES.some((p) => p.key === property);
}

/**
 * Get current_user properties as AppInput array for use in InputWithPlaceholders
 */
export function getCurrentUserInputs(): AppInput[] {
  return CURRENT_USER_PROPERTIES.map((prop) => ({
    title: `${CURRENT_USER_PREFIX}.${prop.key}`,
    required: false,
  }));
}

/**
 * Get the display name for a current_user property
 * e.g., "current_user.firstName" returns "First Name"
 */
export function getCurrentUserPropertyDisplayName(
  title: string
): string | null {
  if (!isCurrentUserPlaceholder(title)) {
    return null;
  }
  const property = title.slice(CURRENT_USER_PREFIX.length + 1);
  const prop = CURRENT_USER_PROPERTIES.find((p) => p.key === property);
  return prop?.displayName ?? null;
}

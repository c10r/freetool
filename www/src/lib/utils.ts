import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Compare users alphabetically by name, falling back to email if name doesn't exist.
 * Handles both `name`/`email` and `userName`/`userEmail` property naming conventions.
 */
export function compareUsersByName(
  a: { name?: string; email?: string; userName?: string; userEmail?: string },
  b: { name?: string; email?: string; userName?: string; userEmail?: string }
): number {
  const nameA = a.name || a.userName || a.email || a.userEmail || "";
  const nameB = b.name || b.userName || b.email || b.userEmail || "";
  return nameA.localeCompare(nameB);
}

import { isDevModeEnabled } from "@/api/api";

export function DevModeBanner() {
  if (!isDevModeEnabled()) {
    return null;
  }

  return (
    <div className="bg-yellow-400 text-yellow-900 text-center text-sm py-1 font-medium">
      DEVELOPMENT MODE - Authentication is mocked
    </div>
  );
}

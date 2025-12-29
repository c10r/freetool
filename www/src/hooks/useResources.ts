import { useQuery } from "@tanstack/react-query";
import { getResources } from "@/api/api";

export function useResources() {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["resources"],
    queryFn: async () => {
      const response = await getResources();
      if (response.error || !response.data) {
        throw new Error("Failed to fetch resources");
      }
      return response.data.items || [];
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  return {
    resources: data || [],
    hasResources: (data?.length ?? 0) > 0,
    isLoading,
    error,
    refetch,
  };
}

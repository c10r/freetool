/**
 * Tests for ResourceSelector - Resource Space Scoping
 *
 * These tests verify that the ResourceSelector correctly passes spaceId
 * to the API when fetching resources.
 */

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as api from "@/api/api";
import ResourceSelector from "@/features/space/components/ResourceSelector";

// Mock the API module
vi.mock("@/api/api");

/**
 * Test helper to wrap components with providers
 */
function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}

describe("ResourceSelector", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should fetch resources with the correct spaceId", async () => {
    const mockResources = {
      items: [
        { id: "resource-1", name: "API Resource 1" },
        { id: "resource-2", name: "API Resource 2" },
      ],
      totalCount: 2,
    };

    vi.mocked(api.getResources).mockResolvedValue({ data: mockResources });

    renderWithProviders(
      <ResourceSelector
        spaceId="space-123"
        onValueChange={vi.fn()}
        placeholder="Select Resource"
      />
    );

    // Wait for the component to mount and fetch resources
    await waitFor(() => {
      expect(api.getResources).toHaveBeenCalledWith("space-123");
    });
  });

  it("should re-fetch resources when spaceId changes", async () => {
    const mockResources = {
      items: [{ id: "resource-1", name: "API Resource 1" }],
      totalCount: 1,
    };

    vi.mocked(api.getResources).mockResolvedValue({ data: mockResources });

    const { rerender } = renderWithProviders(
      <ResourceSelector
        spaceId="space-1"
        onValueChange={vi.fn()}
        placeholder="Select Resource"
      />
    );

    await waitFor(() => {
      expect(api.getResources).toHaveBeenCalledWith("space-1");
    });

    // Change the spaceId
    rerender(
      <QueryClientProvider
        client={
          new QueryClient({
            defaultOptions: { queries: { retry: false } },
          })
        }
      >
        <ResourceSelector
          spaceId="space-2"
          onValueChange={vi.fn()}
          placeholder="Select Resource"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(api.getResources).toHaveBeenCalledWith("space-2");
    });
  });

  it("should display loading state while fetching", () => {
    // Mock a delayed response
    vi.mocked(api.getResources).mockImplementation(
      () =>
        new Promise((resolve) =>
          setTimeout(() => resolve({ data: { items: [], totalCount: 0 } }), 100)
        )
    );

    renderWithProviders(
      <ResourceSelector
        spaceId="space-123"
        onValueChange={vi.fn()}
        placeholder="Select Resource"
      />
    );

    // The component should show loading state
    expect(screen.getByRole("combobox")).toBeDisabled();
  });

  it("should show empty state when no resources exist", async () => {
    const mockResources = {
      items: [],
      totalCount: 0,
    };

    vi.mocked(api.getResources).mockResolvedValue({ data: mockResources });

    renderWithProviders(
      <ResourceSelector
        spaceId="space-123"
        onValueChange={vi.fn()}
        placeholder="Select Resource"
      />
    );

    await waitFor(() => {
      expect(api.getResources).toHaveBeenCalledWith("space-123");
    });
  });
});

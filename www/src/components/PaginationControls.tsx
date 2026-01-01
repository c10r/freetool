import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { cn } from "@/lib/utils";

interface PaginationControlsProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  className?: string;
}

export function PaginationControls({
  currentPage,
  totalPages,
  onPageChange,
  className,
}: PaginationControlsProps) {
  const hasPreviousPage = currentPage > 1;
  const hasNextPage = currentPage < totalPages;

  // Generate page numbers to display with unique keys
  type PageItem =
    | { type: "page"; page: number }
    | { type: "ellipsis"; key: string };

  const getPageItems = (): PageItem[] => {
    const items: PageItem[] = [];

    if (totalPages <= 7) {
      // Show all pages if 7 or fewer
      for (let i = 1; i <= totalPages; i++) {
        items.push({ type: "page", page: i });
      }
    } else {
      // Always show first page
      items.push({ type: "page", page: 1 });

      if (currentPage > 3) {
        items.push({ type: "ellipsis", key: "ellipsis-start" });
      }

      // Show pages around current
      const start = Math.max(2, currentPage - 1);
      const end = Math.min(totalPages - 1, currentPage + 1);

      for (let i = start; i <= end; i++) {
        items.push({ type: "page", page: i });
      }

      if (currentPage < totalPages - 2) {
        items.push({ type: "ellipsis", key: "ellipsis-end" });
      }

      // Always show last page
      items.push({ type: "page", page: totalPages });
    }

    return items;
  };

  const pageItems = getPageItems();

  return (
    <Pagination className={cn("mt-4", className)}>
      <PaginationContent>
        <PaginationItem>
          <PaginationPrevious
            onClick={(e) => {
              e.preventDefault();
              if (hasPreviousPage) {
                onPageChange(currentPage - 1);
              }
            }}
            className={cn(
              "cursor-pointer",
              !hasPreviousPage && "pointer-events-none opacity-50"
            )}
          />
        </PaginationItem>

        {pageItems.map((item) =>
          item.type === "ellipsis" ? (
            <PaginationItem key={item.key}>
              <PaginationEllipsis />
            </PaginationItem>
          ) : (
            <PaginationItem key={item.page}>
              <PaginationLink
                onClick={(e) => {
                  e.preventDefault();
                  onPageChange(item.page);
                }}
                isActive={item.page === currentPage}
                className="cursor-pointer"
              >
                {item.page}
              </PaginationLink>
            </PaginationItem>
          )
        )}

        <PaginationItem>
          <PaginationNext
            onClick={(e) => {
              e.preventDefault();
              if (hasNextPage) {
                onPageChange(currentPage + 1);
              }
            }}
            className={cn(
              "cursor-pointer",
              !hasNextPage && "pointer-events-none opacity-50"
            )}
          />
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
}

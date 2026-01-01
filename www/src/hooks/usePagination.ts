import { useCallback, useState } from "react";

const DEFAULT_PAGE_SIZE = 50;

interface UsePaginationOptions {
  pageSize?: number;
  initialPage?: number;
}

interface UsePaginationResult {
  currentPage: number;
  pageSize: number;
  skip: number;
  totalPages: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  goToPage: (page: number) => void;
  nextPage: () => void;
  previousPage: () => void;
  setTotalCount: (count: number) => void;
  reset: () => void;
}

export function usePagination(
  options?: UsePaginationOptions
): UsePaginationResult {
  const pageSize = options?.pageSize ?? DEFAULT_PAGE_SIZE;
  const initialPage = options?.initialPage ?? 1;

  const [currentPage, setCurrentPage] = useState(initialPage);
  const [totalCount, setTotalCountState] = useState(0);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const skip = (currentPage - 1) * pageSize;
  const hasNextPage = currentPage < totalPages;
  const hasPreviousPage = currentPage > 1;

  const goToPage = useCallback(
    (page: number) => {
      const validPage = Math.max(1, Math.min(page, totalPages));
      setCurrentPage(validPage);
    },
    [totalPages]
  );

  const nextPage = useCallback(() => {
    if (hasNextPage) {
      setCurrentPage((prev) => prev + 1);
    }
  }, [hasNextPage]);

  const previousPage = useCallback(() => {
    if (hasPreviousPage) {
      setCurrentPage((prev) => prev - 1);
    }
  }, [hasPreviousPage]);

  const setTotalCount = useCallback(
    (count: number) => {
      setTotalCountState(count);
      // If current page is now beyond the total pages, reset to page 1
      const newTotalPages = Math.max(1, Math.ceil(count / pageSize));
      if (currentPage > newTotalPages) {
        setCurrentPage(1);
      }
    },
    [currentPage, pageSize]
  );

  const reset = useCallback(() => {
    setCurrentPage(initialPage);
    setTotalCountState(0);
  }, [initialPage]);

  return {
    currentPage,
    pageSize,
    skip,
    totalPages,
    totalCount,
    hasNextPage,
    hasPreviousPage,
    goToPage,
    nextPage,
    previousPage,
    setTotalCount,
    reset,
  };
}

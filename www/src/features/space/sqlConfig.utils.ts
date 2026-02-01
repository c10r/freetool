import type { SqlQueryConfig } from "./types";

export const createDefaultSqlConfig = (): SqlQueryConfig => ({
  mode: "gui",
  table: undefined,
  columns: [],
  filters: [],
  limit: 100,
  orderBy: [],
  rawSql: "",
  rawSqlParams: [],
});

import { useEffect, useMemo, useState } from "react";
import { getSqlColumns, getSqlTables } from "@/api/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { InputWithPlaceholders } from "@/components/ui/input-with-placeholders";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type {
  KeyValuePair,
  SqlFilter,
  SqlFilterOperator,
  SqlOrderBy,
  SqlQueryConfig,
  SqlQueryMode,
} from "../types";
import KeyValueList from "./KeyValueList";

interface SqlQueryConfigFormProps {
  resourceId?: string;
  config: SqlQueryConfig;
  onChange: (config: SqlQueryConfig) => void;
  disabled?: boolean;
  availableInputs: Array<{ title?: string | null; required?: boolean }>;
}

const filterOperators: SqlFilterOperator[] = [
  "=",
  "!=",
  ">",
  ">=",
  "<",
  "<=",
  "IN",
  "NOT IN",
  "LIKE",
  "ILIKE",
  "IS NULL",
  "IS NOT NULL",
];

export default function SqlQueryConfigForm({
  resourceId,
  config,
  onChange,
  disabled = false,
  availableInputs,
}: SqlQueryConfigFormProps) {
  const [tables, setTables] = useState<Array<{ name: string; schema: string }>>(
    []
  );
  const [columns, setColumns] = useState<string[]>([]);
  const [loadingTables, setLoadingTables] = useState(false);
  const [loadingColumns, setLoadingColumns] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedTable = config.table || "";

  const tableOptions = useMemo(
    () =>
      tables.map((table) => ({
        value: `${table.schema}.${table.name}`,
        label: `${table.schema}.${table.name}`,
      })),
    [tables]
  );

  useEffect(() => {
    const fetchTables = async () => {
      if (!resourceId) {
        setTables([]);
        return;
      }

      setLoadingTables(true);
      setError(null);
      try {
        const response = await getSqlTables(resourceId);
        if (response.error) {
          setError("Failed to load SQL tables");
          setTables([]);
        } else {
          setTables(response.data || []);
        }
      } catch (_err) {
        setError("Failed to load SQL tables");
        setTables([]);
      } finally {
        setLoadingTables(false);
      }
    };

    fetchTables();
  }, [resourceId]);

  useEffect(() => {
    const fetchColumns = async () => {
      if (!(resourceId && selectedTable)) {
        setColumns([]);
        return;
      }

      setLoadingColumns(true);
      setError(null);
      try {
        const response = await getSqlColumns(resourceId, selectedTable);
        if (response.error) {
          setError("Failed to load columns");
          setColumns([]);
        } else {
          const columnNames = response.data?.map((column) => column.name) || [];
          setColumns(columnNames);
        }
      } catch (_err) {
        setError("Failed to load columns");
        setColumns([]);
      } finally {
        setLoadingColumns(false);
      }
    };

    fetchColumns();
  }, [resourceId, selectedTable]);

  const updateConfig = (partial: Partial<SqlQueryConfig>) => {
    onChange({ ...config, ...partial });
  };

  const updateFilter = (index: number, partial: Partial<SqlFilter>) => {
    const nextFilters = [...config.filters];
    nextFilters[index] = { ...nextFilters[index], ...partial };
    updateConfig({ filters: nextFilters });
  };

  const updateOrderBy = (index: number, partial: Partial<SqlOrderBy>) => {
    const nextOrderBy = [...config.orderBy];
    nextOrderBy[index] = { ...nextOrderBy[index], ...partial };
    updateConfig({ orderBy: nextOrderBy });
  };

  const toggleColumn = (column: string) => {
    const exists = config.columns.includes(column);
    const nextColumns = exists
      ? config.columns.filter((item) => item !== column)
      : [...config.columns, column];
    updateConfig({ columns: nextColumns });
  };

  const canEdit = !disabled;

  return (
    <div className="space-y-4">
      <Tabs
        value={config.mode}
        onValueChange={(value) => updateConfig({ mode: value as SqlQueryMode })}
      >
        <TabsList>
          <TabsTrigger value="gui">GUI</TabsTrigger>
          <TabsTrigger value="raw">SQL</TabsTrigger>
        </TabsList>
        <TabsContent value="gui" className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="sql-table-select">Table *</Label>
            <Select
              value={selectedTable}
              onValueChange={(value) =>
                updateConfig({
                  table: value || undefined,
                  columns: [],
                  filters: [],
                  orderBy: [],
                })
              }
              disabled={!canEdit || loadingTables}
            >
              <SelectTrigger id="sql-table-select">
                <SelectValue
                  placeholder={
                    loadingTables ? "Loading tables..." : "Select a table"
                  }
                />
              </SelectTrigger>
              <SelectContent>
                {tableOptions.length > 0 ? (
                  tableOptions.map((table) => (
                    <SelectItem key={table.value} value={table.value}>
                      {table.label}
                    </SelectItem>
                  ))
                ) : (
                  <div className="px-2 py-1.5 text-sm text-muted-foreground">
                    {loadingTables
                      ? "Loading tables..."
                      : error || "No tables available"}
                  </div>
                )}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Columns</Label>
            <div className="rounded-md border p-3 max-h-48 overflow-y-auto">
              {columns.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  {loadingColumns
                    ? "Loading columns..."
                    : selectedTable
                      ? "No columns found"
                      : "Select a table to view columns"}
                </p>
              ) : (
                <div className="grid grid-cols-2 gap-2">
                  {columns.map((column) => (
                    <label
                      key={column}
                      className="flex items-center gap-2 text-sm"
                    >
                      <input
                        type="checkbox"
                        className="h-4 w-4"
                        checked={config.columns.includes(column)}
                        onChange={() => toggleColumn(column)}
                        disabled={!canEdit}
                      />
                      <span>{column}</span>
                    </label>
                  ))}
                </div>
              )}
            </div>
            <p className="text-xs text-muted-foreground">
              Leave empty to select all columns.
            </p>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>Filters</Label>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() =>
                  updateConfig({
                    filters: [
                      ...config.filters,
                      { column: "", operator: "=", value: "" },
                    ],
                  })
                }
                disabled={!(canEdit && selectedTable)}
              >
                Add filter
              </Button>
            </div>

            {config.filters.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                Add filters to narrow the results.
              </p>
            ) : (
              <div className="space-y-2">
                {config.filters.map((filter, index) => (
                  <div key={`${filter.column}-${index}`} className="flex gap-2">
                    <Select
                      value={filter.column}
                      onValueChange={(value) =>
                        updateFilter(index, { column: value })
                      }
                      disabled={!canEdit || columns.length === 0}
                    >
                      <SelectTrigger className="w-52">
                        <SelectValue placeholder="Column" />
                      </SelectTrigger>
                      <SelectContent>
                        {columns.map((column) => (
                          <SelectItem key={column} value={column}>
                            {column}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>

                    <Select
                      value={filter.operator}
                      onValueChange={(value) =>
                        updateFilter(index, {
                          operator: value as SqlFilterOperator,
                        })
                      }
                      disabled={!canEdit}
                    >
                      <SelectTrigger className="w-36">
                        <SelectValue placeholder="Operator" />
                      </SelectTrigger>
                      <SelectContent>
                        {filterOperators.map((operator) => (
                          <SelectItem key={operator} value={operator}>
                            {operator}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>

                    <div className="flex-1">
                      <InputWithPlaceholders
                        value={filter.value || ""}
                        onChange={(value) => updateFilter(index, { value })}
                        availableInputs={availableInputs}
                        placeholder={
                          filter.operator === "IN" ||
                          filter.operator === "NOT IN"
                            ? "value1, value2"
                            : "value"
                        }
                        disabled={
                          !canEdit ||
                          filter.operator === "IS NULL" ||
                          filter.operator === "IS NOT NULL"
                        }
                      />
                    </div>

                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() =>
                        updateConfig({
                          filters: config.filters.filter((_, i) => i !== index),
                        })
                      }
                      disabled={!canEdit}
                    >
                      X
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="sql-limit">Limit</Label>
              <Input
                id="sql-limit"
                value={config.limit ?? ""}
                onChange={(e) =>
                  updateConfig({
                    limit: e.target.value ? Number(e.target.value) : undefined,
                  })
                }
                inputMode="numeric"
                placeholder="100"
                disabled={!canEdit}
              />
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label>Order By</Label>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    updateConfig({
                      orderBy: [
                        ...config.orderBy,
                        { column: "", direction: "ASC" },
                      ],
                    })
                  }
                  disabled={!canEdit || columns.length === 0}
                >
                  Add
                </Button>
              </div>

              {config.orderBy.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  Optional sorting.
                </p>
              ) : (
                <div className="space-y-2">
                  {config.orderBy.map((orderBy, index) => (
                    <div
                      key={`${orderBy.column}-${index}`}
                      className="flex gap-2"
                    >
                      <Select
                        value={orderBy.column}
                        onValueChange={(value) =>
                          updateOrderBy(index, { column: value })
                        }
                        disabled={!canEdit || columns.length === 0}
                      >
                        <SelectTrigger className="w-52">
                          <SelectValue placeholder="Column" />
                        </SelectTrigger>
                        <SelectContent>
                          {columns.map((column) => (
                            <SelectItem key={column} value={column}>
                              {column}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>

                      <Select
                        value={orderBy.direction}
                        onValueChange={(value) =>
                          updateOrderBy(index, {
                            direction: value as SqlOrderBy["direction"],
                          })
                        }
                        disabled={!canEdit}
                      >
                        <SelectTrigger className="w-24">
                          <SelectValue placeholder="Dir" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="ASC">ASC</SelectItem>
                          <SelectItem value="DESC">DESC</SelectItem>
                        </SelectContent>
                      </Select>

                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() =>
                          updateConfig({
                            orderBy: config.orderBy.filter(
                              (_, i) => i !== index
                            ),
                          })
                        }
                        disabled={!canEdit}
                      >
                        X
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </TabsContent>

        <TabsContent value="raw" className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="raw-sql">SQL *</Label>
            <InputWithPlaceholders
              id="raw-sql"
              value={config.rawSql || ""}
              onChange={(value) => updateConfig({ rawSql: value })}
              placeholder="select * from users where id = @id"
              availableInputs={availableInputs}
              disabled={!canEdit}
              inputClassName="min-h-[160px]"
              aria-label="SQL"
            />
            <p className="text-xs text-muted-foreground">
              Use @param names and define values below. Expressions like
              {" {{ }}"} and @input are supported.
            </p>
          </div>

          <div className="space-y-2">
            <Label>SQL Parameters</Label>
            <KeyValueList
              items={config.rawSqlParams}
              onChange={(items: KeyValuePair[]) =>
                updateConfig({ rawSqlParams: items })
              }
              ariaLabel="SQL parameters"
              disabled={!canEdit}
              availableInputs={availableInputs}
            />
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}

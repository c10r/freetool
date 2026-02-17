export type FieldType =
  | "text"
  | "email"
  | "date"
  | "integer"
  | "currency"
  | "boolean"
  | "radio";

export type SupportedCurrency = "USD";

export interface RadioOption {
  value: string;
  label?: string;
}

export interface AppField {
  id: string;
  label: string;
  description?: string;
  type: FieldType;
  currency?: SupportedCurrency; // Used only for currency type
  required?: boolean;
  options?: RadioOption[]; // Used only for radio type
  defaultValue?: string; // Only applicable when required is false
}

export type NodeType = "folder" | "app" | "dashboard";

export interface BaseNode {
  id: string;
  name: string;
  type: NodeType;
  parentId?: string;
}

export interface FolderNode extends BaseNode {
  type: "folder";
  childrenIds: string[];
  spaceId?: string; // ID of the space this folder belongs to
  isSpace?: boolean; // true if this folder IS a space (parentId === null)
}

export type EndpointMethod =
  | "GET"
  | "POST"
  | "PUT"
  | "PATCH"
  | "DELETE"
  | "HEAD"
  | "OPTIONS";

export interface KeyValuePair {
  key: string;
  value: string;
}

export type ResourceKind = "http" | "sql";

export type SqlQueryMode = "gui" | "raw";

export type SqlFilterOperator =
  | "="
  | "!="
  | ">"
  | ">="
  | "<"
  | "<="
  | "IN"
  | "NOT IN"
  | "LIKE"
  | "ILIKE"
  | "IS NULL"
  | "IS NOT NULL";

export type SqlSortDirection = "ASC" | "DESC";

export interface SqlFilter {
  column: string;
  operator: SqlFilterOperator;
  value?: string;
}

export interface SqlOrderBy {
  column: string;
  direction: SqlSortDirection;
}

export interface SqlQueryConfig {
  mode: SqlQueryMode;
  table?: string;
  columns: string[];
  filters: SqlFilter[];
  limit?: number;
  orderBy: SqlOrderBy[];
  rawSql?: string;
  rawSqlParams: KeyValuePair[];
}

export type DatabaseAuthScheme = "username_password";

export type DatabaseEngine = "postgres";

export interface DatabaseConfig {
  databaseName: string;
  host: string;
  port: string;
  engine: DatabaseEngine;
  authScheme: DatabaseAuthScheme;
  username: string;
  password: string;
  useSsl: boolean;
  enableSshTunnel: boolean;
  connectionOptions: KeyValuePair[];
}

// Authorization configuration types
export type AuthType = "none" | "basic" | "bearer";

export type AuthConfig =
  | { type: "none" }
  | { type: "basic"; username: string; password: string; isRedacted?: boolean }
  | { type: "bearer"; token: string; isRedacted?: boolean };

export interface Endpoint {
  id: string;
  name: string;
  url: string;
  method: EndpointMethod;
  headers?: KeyValuePair[];
  query?: KeyValuePair[];
  body?: KeyValuePair[];
}

export interface AppNode extends BaseNode {
  type: "app";
  fields: AppField[];
  endpointId?: string; // selected endpoint to call on submit
  resourceId?: string; // selected resource to use for the app
  httpMethod?: EndpointMethod; // HTTP method for the app (GET, POST, etc.)
  urlPath?: string; // custom URL path to append to resource's base URL
  urlParameters?: KeyValuePair[]; // query parameters for the app
  headers?: KeyValuePair[]; // headers for the app
  body?: KeyValuePair[]; // JSON body for the app
  useDynamicJsonBody?: boolean; // when true, body is provided at runtime
  sqlConfig?: SqlQueryConfig; // SQL config for SQL resources
  description?: string; // optional description for the app
}

export type DashboardSection = "left" | "center" | "right";

export type DashboardBindingSourceType =
  | "load_input"
  | "action_input"
  | "prepare_output"
  | "literal";

export interface DashboardAction {
  id: string;
  appId: string;
  label: string;
  section: DashboardSection;
  confirmEnabled?: boolean;
  confirmTitle?: string;
  confirmMessage?: string;
  successMessage?: string;
  errorMessage?: string;
}

export interface DashboardBinding {
  id: string;
  actionId: string;
  inputName: string;
  sourceType: DashboardBindingSourceType;
  sourceKey?: string;
  literalValue?: string;
}

export interface DashboardConfig {
  loadInputs: AppField[];
  actionInputs: AppField[];
  actions: DashboardAction[];
  bindings: DashboardBinding[];
  layout: {
    left: string[];
    center: string[];
    right: string[];
  };
}

export interface DashboardNode extends BaseNode {
  type: "dashboard";
  prepareAppId?: string;
  configuration: string;
}

export type SpaceNode = FolderNode | AppNode | DashboardNode;

// Tree node type used for rendering (includes expanded children)
export interface TreeNode extends BaseNode {
  type: NodeType;
  children?: TreeNode[];
}

export interface SpaceMainProps {
  nodes: Record<string, SpaceNode>;
  selectedId: string;
  onSelect: (id: string, targetSpaceId?: string) => void;
  updateNode: (node: SpaceNode) => void;
  insertFolderNode: (folder: FolderNode) => void;
  createFolder: (parentId: string) => void;
  createApp: (
    parentId: string,
    name?: string,
    description?: string,
    resourceId?: string,
    httpMethod?: EndpointMethod,
    urlPath?: string,
    queryParameters?: KeyValuePair[],
    headers?: KeyValuePair[],
    body?: KeyValuePair[],
    inputs?: AppField[],
    useDynamicJsonBody?: boolean,
    sqlConfig?: SqlQueryConfig
  ) => Promise<void>;
  createDashboard: (
    parentId: string,
    name?: string,
    prepareAppId?: string | null,
    configuration?: string
  ) => Promise<void>;
  deleteNode: (id: string) => void;
  endpoints: Record<string, Endpoint>;
  createEndpoint: () => void;
  updateEndpoint: (ep: Endpoint) => void;
  deleteEndpoint: (id: string) => void;
  spaceId: string;
  spaceName?: string;
}

/**
 * Trash / Restore types
 */
export interface TrashItem {
  id: string;
  name: string;
  itemType: "app" | "folder" | "resource";
  spaceId: string;
  deletedAt: string;
}

export interface TrashListDto {
  items: TrashItem[];
  totalCount: number;
}

export interface RestoreResult {
  restoredId: string;
  newName?: string;
  autoRestoredResourceId?: string;
}

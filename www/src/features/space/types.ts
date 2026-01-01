export type FieldType = "text" | "email" | "date" | "integer" | "boolean";

export interface AppField {
  id: string;
  label: string;
  type: FieldType;
  required?: boolean;
}

export type NodeType = "folder" | "app";

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
}

export type SpaceNode = FolderNode | AppNode;

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
    resourceId?: string,
    httpMethod?: EndpointMethod,
    urlPath?: string,
    queryParameters?: KeyValuePair[],
    headers?: KeyValuePair[],
    body?: KeyValuePair[],
    inputs?: AppField[]
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

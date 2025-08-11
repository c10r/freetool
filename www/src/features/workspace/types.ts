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
}

export type WorkspaceNode = FolderNode | AppNode;

export interface WorkspaceMainProps {
  nodes: Record<string, WorkspaceNode>;
  selectedId: string;
  onSelect: (id: string) => void;
  updateNode: (node: WorkspaceNode) => void;
  createFolder: (parentId: string) => void;
  createApp: (parentId: string) => void;
  deleteNode: (id: string) => void;
  endpoints: Record<string, Endpoint>;
  createEndpoint: () => void;
  updateEndpoint: (ep: Endpoint) => void;
  deleteEndpoint: (id: string) => void;
}

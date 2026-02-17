import {
  Check,
  Edit,
  Eye,
  Loader2,
  Play,
  Plus,
  RotateCcw,
  Save,
  ShieldAlert,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  prepareDashboard,
  runDashboardAction,
  updateDashboardConfiguration,
  updateDashboardName,
  updateDashboardPrepareApp,
} from "@/api/api";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Switch } from "@/components/ui/switch";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { useHasPermission } from "@/hooks/usePermissions";
import type {
  AppNode,
  DashboardAction,
  DashboardBinding,
  DashboardBindingSourceType,
  DashboardConfig,
  DashboardNode,
  DashboardSection,
  FieldType,
  SpaceNode,
} from "../types";
import InputFieldEditor from "./InputFieldEditor";

const DASHBOARD_SECTIONS: DashboardSection[] = ["left", "center", "right"];
const BINDING_SOURCE_TYPES: DashboardBindingSourceType[] = [
  "load_input",
  "action_input",
  "prepare_output",
  "literal",
];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toOptionalString(value: unknown): string | undefined {
  if (typeof value !== "string") {
    return undefined;
  }
  const trimmedValue = value.trim();
  return trimmedValue.length > 0 ? trimmedValue : undefined;
}

function toBoolean(value: unknown): boolean {
  return typeof value === "boolean" ? value : false;
}

function toFieldType(value: unknown): FieldType {
  const supportedTypes: FieldType[] = [
    "text",
    "email",
    "date",
    "integer",
    "currency",
    "boolean",
    "radio",
  ];
  if (
    typeof value === "string" &&
    supportedTypes.includes(value as FieldType)
  ) {
    return value as FieldType;
  }
  return "text";
}

function parseDashboardConfig(configuration: string): DashboardConfig {
  const defaultConfig: DashboardConfig = {
    loadInputs: [],
    actionInputs: [],
    actions: [],
    bindings: [],
    layout: {
      left: [],
      center: [],
      right: [],
    },
  };

  if (!configuration.trim()) {
    return defaultConfig;
  }

  try {
    const parsed = JSON.parse(configuration) as unknown;
    if (!isRecord(parsed)) {
      return defaultConfig;
    }

    const parseInputFields = (value: unknown) => {
      if (!Array.isArray(value)) {
        return [];
      }

      return value
        .map((item) => {
          if (!isRecord(item)) {
            return null;
          }

          const type = toFieldType(item.type);
          const required = type === "boolean" ? true : toBoolean(item.required);

          const options = Array.isArray(item.options)
            ? item.options
                .map((option) => {
                  if (!isRecord(option)) {
                    return null;
                  }
                  const optionValue = toOptionalString(option.value);
                  if (!optionValue) {
                    return null;
                  }
                  return {
                    value: optionValue,
                    label: toOptionalString(option.label),
                  };
                })
                .filter(
                  (option): option is { value: string; label?: string } =>
                    option !== null
                )
            : undefined;

          return {
            id:
              toOptionalString(item.id) ||
              toOptionalString(item.key) ||
              crypto.randomUUID(),
            label:
              toOptionalString(item.label) ||
              toOptionalString(item.title) ||
              toOptionalString(item.name) ||
              "",
            description: toOptionalString(item.description),
            type,
            required,
            options,
            defaultValue:
              type === "boolean"
                ? undefined
                : toOptionalString(item.defaultValue),
          };
        })
        .filter(
          (item): item is DashboardConfig["loadInputs"][number] => item !== null
        );
    };

    const parsedActions = Array.isArray(parsed.actions)
      ? parsed.actions
          .map((action) => {
            if (!isRecord(action)) {
              return null;
            }

            const confirmConfig = isRecord(action.confirmConfig)
              ? action.confirmConfig
              : undefined;
            const section = toOptionalString(action.section);

            return {
              id:
                toOptionalString(action.id) ||
                toOptionalString(action.actionId) ||
                crypto.randomUUID(),
              appId: toOptionalString(action.appId) || "",
              label: toOptionalString(action.label) || "",
              section:
                section &&
                DASHBOARD_SECTIONS.includes(section as DashboardSection)
                  ? (section as DashboardSection)
                  : "center",
              confirmEnabled: confirmConfig !== undefined,
              confirmTitle:
                confirmConfig && isRecord(confirmConfig)
                  ? toOptionalString(confirmConfig.title)
                  : undefined,
              confirmMessage:
                confirmConfig && isRecord(confirmConfig)
                  ? toOptionalString(confirmConfig.message)
                  : undefined,
              successMessage: toOptionalString(action.successMessage),
              errorMessage: toOptionalString(action.errorMessage),
            };
          })
          .filter((action): action is DashboardAction => action !== null)
      : [];

    const parsedBindings = Array.isArray(parsed.bindings)
      ? parsed.bindings
          .map((binding) => {
            if (!isRecord(binding)) {
              return null;
            }

            const sourceType = toOptionalString(binding.sourceType);
            const normalizedSourceType =
              sourceType &&
              BINDING_SOURCE_TYPES.includes(
                sourceType as DashboardBindingSourceType
              )
                ? (sourceType as DashboardBindingSourceType)
                : "literal";

            return {
              id: toOptionalString(binding.id) || crypto.randomUUID(),
              actionId: toOptionalString(binding.actionId) || "",
              inputName:
                toOptionalString(binding.inputName) ||
                toOptionalString(binding.targetInput) ||
                "",
              sourceType: normalizedSourceType,
              sourceKey:
                toOptionalString(binding.sourceKey) ||
                toOptionalString(binding.key),
              literalValue: toOptionalString(binding.literalValue),
            };
          })
          .filter((binding): binding is DashboardBinding => binding !== null)
      : [];

    const layoutRecord = isRecord(parsed.layout) ? parsed.layout : null;
    const parseLayoutSide = (key: "left" | "center" | "right") => {
      if (!(layoutRecord && Array.isArray(layoutRecord[key]))) {
        return parsedActions
          .filter((action) => action.section === key)
          .map((action) => action.id);
      }
      return layoutRecord[key]
        .map((item) => (typeof item === "string" ? item : null))
        .filter((item): item is string => item !== null);
    };

    return {
      loadInputs: parseInputFields(parsed.loadInputs),
      actionInputs: parseInputFields(parsed.actionInputs),
      actions: parsedActions,
      bindings: parsedBindings,
      layout: {
        left: parseLayoutSide("left"),
        center: parseLayoutSide("center"),
        right: parseLayoutSide("right"),
      },
    };
  } catch {
    return defaultConfig;
  }
}

function serializeDashboardConfig(config: DashboardConfig): string {
  const layout = {
    left: config.actions
      .filter((action) => action.section === "left")
      .map((action) => action.id),
    center: config.actions
      .filter((action) => action.section === "center")
      .map((action) => action.id),
    right: config.actions
      .filter((action) => action.section === "right")
      .map((action) => action.id),
  };

  const payload = {
    version: 1,
    loadInputs: config.loadInputs,
    actionInputs: config.actionInputs,
    actions: config.actions.map((action) => ({
      id: action.id,
      appId: action.appId,
      label: action.label || undefined,
      section: action.section,
      confirmConfig: action.confirmEnabled
        ? {
            title: action.confirmTitle || "Confirm Action",
            message:
              action.confirmMessage || "Are you sure you want to continue?",
          }
        : undefined,
      successMessage: action.successMessage || undefined,
      errorMessage: action.errorMessage || undefined,
    })),
    bindings: config.bindings.map((binding) => ({
      actionId: binding.actionId,
      inputName: binding.inputName,
      sourceType: binding.sourceType,
      sourceKey:
        binding.sourceType === "literal" ? undefined : binding.sourceKey,
      literalValue:
        binding.sourceType === "literal"
          ? binding.literalValue || ""
          : undefined,
    })),
    layout,
  };

  return JSON.stringify(payload, null, 2);
}

function getNodeSpaceId(
  node: SpaceNode | undefined,
  nodes: Record<string, SpaceNode>
): string | undefined {
  if (!node) {
    return undefined;
  }

  if (node.type === "folder") {
    return node.spaceId;
  }

  return getNodeSpaceId(
    node.parentId ? nodes[node.parentId] : undefined,
    nodes
  );
}

function getConfigSnapshot(input: {
  name: string;
  prepareAppId?: string;
  configuration: DashboardConfig;
}): string {
  return JSON.stringify({
    name: input.name.trim(),
    prepareAppId: input.prepareAppId || null,
    configuration: input.configuration,
  });
}

type RuntimeValues = Record<string, string>;

interface RuntimeActionResult {
  actionRunId?: string;
  status?: string;
  response?: string | null;
  errorMessage?: string | null;
}

function getFieldKey(field: DashboardConfig["loadInputs"][number]): string {
  return field.id;
}

function getDefaultRuntimeValue(field: DashboardConfig["loadInputs"][number]) {
  if (field.type === "boolean") {
    return field.defaultValue === "true" ? "true" : "false";
  }
  return field.defaultValue || "";
}

function getInitialRuntimeValues(
  fields: DashboardConfig["loadInputs"]
): RuntimeValues {
  return fields.reduce<RuntimeValues>((values, field) => {
    values[getFieldKey(field)] = getDefaultRuntimeValue(field);
    return values;
  }, {});
}

function syncRuntimeValues(
  values: RuntimeValues,
  fields: DashboardConfig["loadInputs"]
): RuntimeValues {
  const nextValues: RuntimeValues = {};
  for (const field of fields) {
    const key = getFieldKey(field);
    nextValues[key] =
      values[key] === undefined ? getDefaultRuntimeValue(field) : values[key];
  }
  return nextValues;
}

function isSuccessfulStatus(status: string | undefined): boolean {
  return (status || "").toLowerCase() === "success";
}

function formatResponse(value: string | null | undefined) {
  if (!value) {
    return "";
  }

  try {
    const parsed = JSON.parse(value);
    return JSON.stringify(parsed, null, 2);
  } catch {
    return value;
  }
}

export default function DashboardView({
  dashboard,
  nodes,
  spaceId,
  updateNode,
  mode = "edit",
  runFolderName,
}: {
  dashboard: DashboardNode;
  nodes: Record<string, SpaceNode>;
  spaceId: string;
  updateNode?: (node: SpaceNode) => void;
  mode?: "edit" | "run";
  runFolderName?: string;
}) {
  const navigate = useNavigate();
  const isRunMode = mode === "run";
  const [editingName, setEditingName] = useState(false);
  const [name, setName] = useState(dashboard.name);
  const [prepareAppId, setPrepareAppId] = useState(
    dashboard.prepareAppId || ""
  );
  const [loadInputs, setLoadInputs] = useState<DashboardConfig["loadInputs"]>(
    []
  );
  const [actionInputs, setActionInputs] = useState<
    DashboardConfig["actionInputs"]
  >([]);
  const [actions, setActions] = useState<DashboardAction[]>([]);
  const [bindings, setBindings] = useState<DashboardBinding[]>([]);
  const [initialSnapshot, setInitialSnapshot] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [isSaved, setIsSaved] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [loadInputValues, setLoadInputValues] = useState<RuntimeValues>({});
  const [actionInputValues, setActionInputValues] = useState<RuntimeValues>({});
  const [loadInputErrors, setLoadInputErrors] = useState<
    Record<string, string>
  >({});
  const [actionInputErrors, setActionInputErrors] = useState<
    Record<string, string>
  >({});
  const [isPrepared, setIsPrepared] = useState(false);
  const [prepareRunId, setPrepareRunId] = useState<string | undefined>(
    undefined
  );
  const [prepareStatus, setPrepareStatus] = useState<string | undefined>(
    undefined
  );
  const [prepareResponse, setPrepareResponse] = useState<string | null>(null);
  const [prepareErrorMessage, setPrepareErrorMessage] = useState<string | null>(
    null
  );
  const [runtimeError, setRuntimeError] = useState<string | null>(null);
  const [isPreparing, setIsPreparing] = useState(false);
  const [runningActionId, setRunningActionId] = useState<string | null>(null);
  const [actionResults, setActionResults] = useState<
    Record<string, RuntimeActionResult>
  >({});
  const [confirmActionId, setConfirmActionId] = useState<string | null>(null);
  const [resetConfirmOpen, setResetConfirmOpen] = useState(false);

  const effectiveSpaceId = useMemo(() => {
    const inferredSpaceId = getNodeSpaceId(dashboard, nodes);
    return inferredSpaceId || spaceId;
  }, [dashboard, nodes, spaceId]);
  const runPath = `/spaces/${effectiveSpaceId}/${dashboard.id}/dashboard-run`;
  const inferredFolderName =
    dashboard.parentId && nodes[dashboard.parentId]?.type === "folder"
      ? nodes[dashboard.parentId].name
      : "";

  const canEditDashboard = useHasPermission(effectiveSpaceId, "edit_dashboard");
  const canRunDashboard = useHasPermission(effectiveSpaceId, "run_dashboard");
  const canRunApp = useHasPermission(effectiveSpaceId, "run_app");
  const canRunDashboardRuntime = canRunDashboard && canRunApp;

  const availableApps = useMemo(() => {
    return Object.values(nodes)
      .filter((node): node is AppNode => {
        if (node.type !== "app") {
          return false;
        }
        return getNodeSpaceId(node, nodes) === effectiveSpaceId;
      })
      .sort((left, right) => left.name.localeCompare(right.name));
  }, [nodes, effectiveSpaceId]);

  useEffect(() => {
    const parsedConfig = parseDashboardConfig(dashboard.configuration);
    const nextName = dashboard.name;
    const nextPrepareAppId = dashboard.prepareAppId || "";

    setName(nextName);
    setPrepareAppId(nextPrepareAppId);
    setLoadInputs(parsedConfig.loadInputs);
    setActionInputs(parsedConfig.actionInputs);
    setActions(parsedConfig.actions);
    setBindings(parsedConfig.bindings);
    setLoadInputValues(getInitialRuntimeValues(parsedConfig.loadInputs));
    setActionInputValues(getInitialRuntimeValues(parsedConfig.actionInputs));
    setLoadInputErrors({});
    setActionInputErrors({});
    setIsPrepared(false);
    setPrepareRunId(undefined);
    setPrepareStatus(undefined);
    setPrepareResponse(null);
    setPrepareErrorMessage(null);
    setRuntimeError(null);
    setIsPreparing(false);
    setRunningActionId(null);
    setActionResults({});
    setConfirmActionId(null);
    setResetConfirmOpen(false);
    setEditingName(false);
    setSaveError(null);

    setInitialSnapshot(
      getConfigSnapshot({
        name: nextName,
        prepareAppId: nextPrepareAppId,
        configuration: parsedConfig,
      })
    );
  }, [dashboard]);

  const dashboardConfig = useMemo<DashboardConfig>(
    () => ({
      loadInputs,
      actionInputs,
      actions,
      bindings,
      layout: {
        left: actions
          .filter((action) => action.section === "left")
          .map((action) => action.id),
        center: actions
          .filter((action) => action.section === "center")
          .map((action) => action.id),
        right: actions
          .filter((action) => action.section === "right")
          .map((action) => action.id),
      },
    }),
    [loadInputs, actionInputs, actions, bindings]
  );

  useEffect(() => {
    setLoadInputValues((previousValues) =>
      syncRuntimeValues(previousValues, loadInputs)
    );
    setLoadInputErrors((previousErrors) =>
      Object.fromEntries(
        Object.entries(previousErrors).filter(([fieldId]) =>
          loadInputs.some((field) => field.id === fieldId)
        )
      )
    );
  }, [loadInputs]);

  useEffect(() => {
    setActionInputValues((previousValues) =>
      syncRuntimeValues(previousValues, actionInputs)
    );
    setActionInputErrors((previousErrors) =>
      Object.fromEntries(
        Object.entries(previousErrors).filter(([fieldId]) =>
          actionInputs.some((field) => field.id === fieldId)
        )
      )
    );
  }, [actionInputs]);

  const hasUnsavedChanges = useMemo(() => {
    const currentSnapshot = getConfigSnapshot({
      name,
      prepareAppId,
      configuration: dashboardConfig,
    });
    return currentSnapshot !== initialSnapshot;
  }, [name, prepareAppId, dashboardConfig, initialSnapshot]);

  const handleAddAction = () => {
    setActions((previousActions) => [
      ...previousActions,
      {
        id: crypto.randomUUID(),
        appId: "",
        label: "",
        section: "center",
      },
    ]);
  };

  const handleUpdateAction = (
    actionId: string,
    updates: Partial<DashboardAction>
  ) => {
    setActions((previousActions) =>
      previousActions.map((action) =>
        action.id === actionId ? { ...action, ...updates } : action
      )
    );
  };

  const handleDeleteAction = (actionId: string) => {
    setActions((previousActions) =>
      previousActions.filter((action) => action.id !== actionId)
    );
    setBindings((previousBindings) =>
      previousBindings.filter((binding) => binding.actionId !== actionId)
    );
  };

  const handleAddBinding = () => {
    setBindings((previousBindings) => [
      ...previousBindings,
      {
        id: crypto.randomUUID(),
        actionId: actions[0]?.id || "",
        inputName: "",
        sourceType: "literal",
        literalValue: "",
      },
    ]);
  };

  const handleUpdateBinding = (
    bindingId: string,
    updates: Partial<DashboardBinding>
  ) => {
    setBindings((previousBindings) =>
      previousBindings.map((binding) =>
        binding.id === bindingId ? { ...binding, ...updates } : binding
      )
    );
  };

  const handleDeleteBinding = (bindingId: string) => {
    setBindings((previousBindings) =>
      previousBindings.filter((binding) => binding.id !== bindingId)
    );
  };

  const handleSave = async () => {
    if (!(canEditDashboard && hasUnsavedChanges)) {
      return;
    }

    const trimmedName = name.trim();
    if (!trimmedName) {
      setSaveError("Dashboard name is required");
      return;
    }

    setIsSaving(true);
    setSaveError(null);

    try {
      if (trimmedName !== dashboard.name) {
        const nameResponse = await updateDashboardName(
          dashboard.id,
          trimmedName
        );
        if (nameResponse.error) {
          throw new Error(
            nameResponse.error.message || "Failed to update name"
          );
        }
      }

      const normalizedPrepareAppId = prepareAppId || null;
      if ((dashboard.prepareAppId || null) !== normalizedPrepareAppId) {
        const prepareResponse = await updateDashboardPrepareApp(
          dashboard.id,
          normalizedPrepareAppId
        );
        if (prepareResponse.error) {
          throw new Error(
            prepareResponse.error.message || "Failed to update prepare app"
          );
        }
      }

      const configuration = serializeDashboardConfig(dashboardConfig);
      const configResponse = await updateDashboardConfiguration(
        dashboard.id,
        configuration
      );
      if (configResponse.error) {
        throw new Error(
          configResponse.error.message || "Failed to update configuration"
        );
      }

      updateNode?.({
        ...dashboard,
        name: trimmedName,
        prepareAppId: normalizedPrepareAppId || undefined,
        configuration,
      });

      setInitialSnapshot(
        getConfigSnapshot({
          name: trimmedName,
          prepareAppId: normalizedPrepareAppId || undefined,
          configuration: dashboardConfig,
        })
      );

      setIsSaved(true);
      setTimeout(() => {
        setIsSaved(false);
      }, 2000);
    } catch (error) {
      setSaveError(
        error instanceof Error ? error.message : "Failed to save dashboard"
      );
    } finally {
      setIsSaving(false);
    }
  };

  const runtimeInteractionLocked = isPreparing || runningActionId !== null;
  const selectedConfirmAction =
    actions.find((action) => action.id === confirmActionId) || null;

  const actionsBySection = useMemo(
    () => ({
      left: actions.filter((action) => action.section === "left"),
      center: actions.filter((action) => action.section === "center"),
      right: actions.filter((action) => action.section === "right"),
    }),
    [actions]
  );

  const getRunInputPayload = (
    fields: DashboardConfig["loadInputs"],
    values: RuntimeValues
  ) => {
    return fields.map((field) => ({
      title: field.label.trim(),
      value: values[getFieldKey(field)] ?? "",
    }));
  };

  const validateRuntimeFields = (
    fields: DashboardConfig["loadInputs"],
    values: RuntimeValues
  ) => {
    const errors: Record<string, string> = {};
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    for (const field of fields) {
      const key = getFieldKey(field);
      const label = field.label.trim();
      const value = values[key] ?? "";

      if (!label) {
        errors[key] = "Field label is required in dashboard editor";
        continue;
      }

      if (field.required && field.type !== "boolean" && !value.trim()) {
        errors[key] = "This field is required";
        continue;
      }

      if (
        field.type === "email" &&
        value.trim() &&
        !emailPattern.test(value.trim())
      ) {
        errors[key] = "Enter a valid email address";
        continue;
      }

      if (field.type === "integer" && value.trim() && !/^-?\d+$/.test(value)) {
        errors[key] = "Enter a valid integer";
        continue;
      }

      if (
        field.type === "currency" &&
        value.trim() &&
        !/^(?:0|[1-9]\d*)(?:\.\d{1,2})?$/.test(value.trim())
      ) {
        errors[key] = "Enter a valid currency amount (max 2 decimals)";
        continue;
      }

      if (
        field.type === "radio" &&
        value &&
        !(field.options || []).some((option) => option.value === value)
      ) {
        errors[key] = "Select one of the available options";
      }
    }

    return errors;
  };

  const resetRuntimeState = () => {
    setIsPrepared(false);
    setPrepareRunId(undefined);
    setPrepareStatus(undefined);
    setPrepareResponse(null);
    setPrepareErrorMessage(null);
    setRuntimeError(null);
    setIsPreparing(false);
    setRunningActionId(null);
    setActionResults({});
    setConfirmActionId(null);
    setResetConfirmOpen(false);
    setLoadInputErrors({});
    setActionInputErrors({});
    setLoadInputValues(getInitialRuntimeValues(loadInputs));
    setActionInputValues(getInitialRuntimeValues(actionInputs));
  };

  const handlePrepareDashboard = async () => {
    if (runtimeInteractionLocked || !canRunDashboardRuntime) {
      return;
    }

    const nextLoadInputErrors = validateRuntimeFields(
      loadInputs,
      loadInputValues
    );
    setLoadInputErrors(nextLoadInputErrors);
    if (Object.keys(nextLoadInputErrors).length > 0) {
      return;
    }

    setRuntimeError(null);
    setPrepareErrorMessage(null);
    setPrepareResponse(null);
    setPrepareStatus(undefined);
    setIsPreparing(true);
    setActionResults({});

    if (!prepareAppId) {
      setIsPrepared(true);
      setPrepareRunId(undefined);
      setPrepareStatus("Success");
      setIsPreparing(false);
      return;
    }

    try {
      const response = await prepareDashboard(
        dashboard.id,
        getRunInputPayload(loadInputs, loadInputValues)
      );

      if (response.error || !response.data) {
        throw new Error(response.error?.message || "Failed to load dashboard");
      }

      const nextStatus = response.data.status;
      const nextPrepareRunId = response.data.prepareRunId;
      const nextResponse = response.data.response || null;
      const nextErrorMessage = response.data.errorMessage || null;

      setPrepareStatus(nextStatus);
      setPrepareRunId(nextPrepareRunId);
      setPrepareResponse(nextResponse);
      setPrepareErrorMessage(nextErrorMessage);

      if (!isSuccessfulStatus(nextStatus)) {
        setIsPrepared(false);
        setRuntimeError(
          nextErrorMessage ||
            "Load Dashboard failed. Fix inputs and try loading again."
        );
        return;
      }

      setIsPrepared(true);
    } catch (error) {
      setIsPrepared(false);
      setPrepareRunId(undefined);
      setRuntimeError(
        error instanceof Error ? error.message : "Failed to load dashboard"
      );
    } finally {
      setIsPreparing(false);
    }
  };

  const executeAction = async (action: DashboardAction) => {
    if (runtimeInteractionLocked || !canRunDashboardRuntime || !isPrepared) {
      return;
    }

    const nextLoadInputErrors = validateRuntimeFields(
      loadInputs,
      loadInputValues
    );
    const nextActionInputErrors = validateRuntimeFields(
      actionInputs,
      actionInputValues
    );
    setLoadInputErrors(nextLoadInputErrors);
    setActionInputErrors(nextActionInputErrors);

    if (
      Object.keys(nextLoadInputErrors).length > 0 ||
      Object.keys(nextActionInputErrors).length > 0
    ) {
      return;
    }

    setRuntimeError(null);
    setRunningActionId(action.id);

    try {
      const response = await runDashboardAction({
        dashboardId: dashboard.id,
        actionId: action.id,
        prepareRunId,
        loadInputs: getRunInputPayload(loadInputs, loadInputValues),
        actionInputs: getRunInputPayload(actionInputs, actionInputValues),
      });

      if (response.error || !response.data) {
        throw new Error(
          response.error?.message || `Failed to run ${action.label || "action"}`
        );
      }

      setActionResults((previousResults) => ({
        ...previousResults,
        [action.id]: response.data,
      }));
    } catch (error) {
      setRuntimeError(
        error instanceof Error ? error.message : "Failed to run action"
      );
    } finally {
      setRunningActionId(null);
      setConfirmActionId(null);
    }
  };

  const handleActionRun = async (action: DashboardAction) => {
    if (action.confirmEnabled) {
      setConfirmActionId(action.id);
      return;
    }
    await executeAction(action);
  };

  const renderRuntimeInputField = (
    field: DashboardConfig["loadInputs"][number],
    values: RuntimeValues,
    onValueChange: (fieldId: string, value: string) => void,
    disabled: boolean,
    error: string | undefined
  ) => {
    const key = getFieldKey(field);
    const value = values[key] ?? "";
    const fieldId = `runtime-input-${field.id}`;

    return (
      <div key={field.id} className="space-y-2">
        <Label htmlFor={fieldId}>
          {field.label || "Untitled field"}
          {field.required ? " *" : ""}
        </Label>
        {field.description && (
          <p className="text-xs text-muted-foreground">{field.description}</p>
        )}
        {field.type === "boolean" ? (
          <div className="flex items-center gap-2">
            <Switch
              id={fieldId}
              checked={value === "true"}
              onCheckedChange={(checked) =>
                onValueChange(field.id, checked ? "true" : "false")
              }
              disabled={disabled}
            />
            <span className="text-sm text-muted-foreground">
              {value === "true" ? "Enabled" : "Disabled"}
            </span>
          </div>
        ) : field.type === "radio" ? (
          <RadioGroup
            value={value}
            onValueChange={(nextValue) => onValueChange(field.id, nextValue)}
            disabled={disabled}
          >
            {(field.options || []).map((option) => (
              <div key={option.value} className="flex items-center space-x-2">
                <RadioGroupItem
                  value={option.value}
                  id={`${fieldId}-${option.value}`}
                />
                <Label
                  htmlFor={`${fieldId}-${option.value}`}
                  className="font-normal"
                >
                  {option.label || option.value}
                </Label>
              </div>
            ))}
          </RadioGroup>
        ) : (
          <Input
            id={fieldId}
            type={
              field.type === "integer"
                ? "number"
                : field.type === "currency"
                  ? "number"
                  : field.type === "date"
                    ? "date"
                    : field.type === "email"
                      ? "email"
                      : "text"
            }
            min={field.type === "currency" ? 0 : undefined}
            step={
              field.type === "integer"
                ? 1
                : field.type === "currency"
                  ? 0.01
                  : undefined
            }
            value={value}
            onChange={(event) => onValueChange(field.id, event.target.value)}
            disabled={disabled}
          />
        )}
        {error && <p className="text-sm text-red-600">{error}</p>}
      </div>
    );
  };

  return (
    <section className="p-6 space-y-4 overflow-y-auto flex-1">
      <header className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          {isRunMode ? (
            <Breadcrumb>
              <BreadcrumbList>
                <BreadcrumbItem>
                  <BreadcrumbLink asChild>
                    <Link
                      to={
                        effectiveSpaceId
                          ? `/spaces/${effectiveSpaceId}`
                          : "/spaces"
                      }
                    >
                      Spaces
                    </Link>
                  </BreadcrumbLink>
                </BreadcrumbItem>
                {(runFolderName || inferredFolderName) &&
                  dashboard.parentId &&
                  effectiveSpaceId && (
                    <>
                      <BreadcrumbSeparator />
                      <BreadcrumbItem>
                        <BreadcrumbLink asChild>
                          <Link
                            to={`/spaces/${effectiveSpaceId}/${dashboard.parentId}`}
                          >
                            {runFolderName || inferredFolderName}
                          </Link>
                        </BreadcrumbLink>
                      </BreadcrumbItem>
                    </>
                  )}
                <BreadcrumbSeparator />
                <BreadcrumbItem>
                  <BreadcrumbPage>{dashboard.name}</BreadcrumbPage>
                </BreadcrumbItem>
              </BreadcrumbList>
            </Breadcrumb>
          ) : editingName ? (
            <div className="flex items-center gap-2">
              <Input
                value={name}
                onChange={(event) => setName(event.target.value)}
                className="w-80"
                disabled={isSaving || !canEditDashboard}
              />
              <Button
                variant="outline"
                onClick={() => {
                  setName(dashboard.name);
                  setEditingName(false);
                }}
                disabled={isSaving}
              >
                Cancel
              </Button>
            </div>
          ) : (
            <>
              <h2 className="text-2xl font-semibold">{dashboard.name}</h2>
              {!canEditDashboard && (
                <Badge variant="secondary">
                  <Eye className="w-3 h-3 mr-1" />
                  View Only
                </Badge>
              )}
            </>
          )}

          {!isRunMode && canEditDashboard && (
            <Button
              variant="secondary"
              size="icon"
              onClick={() => setEditingName((isEditing) => !isEditing)}
              aria-label="Rename dashboard"
            >
              <Edit size={16} />
            </Button>
          )}
        </div>

        <div className="flex items-center gap-2">
          {isRunMode ? (
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="outline" size="icon" asChild>
                    <Link
                      to={`/audit?scope=dashboard&dashboardId=${dashboard.id}`}
                      aria-label="View audit log"
                    >
                      <ShieldAlert className="h-4 w-4" />
                    </Link>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>View Audit Log</TooltipContent>
              </Tooltip>
            </TooltipProvider>
          ) : (
            <>
              <Button
                variant="outline"
                className="gap-2"
                onClick={() => navigate(runPath)}
              >
                <Play className="h-4 w-4" />
                Run Dashboard
              </Button>
              {canEditDashboard && hasUnsavedChanges && (
                <Button
                  onClick={handleSave}
                  disabled={isSaving}
                  className="gap-2"
                >
                  {isSaving ? (
                    "Saving..."
                  ) : (
                    <>
                      <Save className="h-4 w-4" />
                      Save Changes
                    </>
                  )}
                </Button>
              )}
              {canEditDashboard && !hasUnsavedChanges && isSaved && (
                <div className="flex items-center gap-1 text-green-600 text-sm">
                  <Check className="h-4 w-4" />
                  Saved
                </div>
              )}
              {saveError && (
                <div className="flex items-center gap-1 text-red-600 text-sm">
                  <X className="h-4 w-4" />
                  {saveError}
                </div>
              )}
            </>
          )}
        </div>
      </header>

      {!(isRunMode || canEditDashboard) && (
        <div className="bg-muted/50 border border-muted rounded-lg p-4">
          <p className="text-sm text-muted-foreground">
            <Eye className="w-4 h-4 inline mr-2" />
            You have view-only access to this dashboard. To make changes,
            contact your team admin to request edit_dashboard permission.
          </p>
        </div>
      )}

      {isRunMode && !canRunDashboardRuntime && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
          <p className="text-sm text-amber-900">
            <Eye className="w-4 h-4 inline mr-2" />
            This dashboard is read-only at runtime. You need both{" "}
            <code>run_dashboard</code> and <code>run_app</code> permissions to
            load and execute actions.
          </p>
        </div>
      )}

      {isRunMode && (
        <Card>
          <CardHeader>
            <CardTitle>Runtime</CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={isPrepared ? "default" : "secondary"}>
                {isPrepared ? "Post-load" : "Pre-load"}
              </Badge>
              {prepareStatus && (
                <Badge
                  variant={
                    isSuccessfulStatus(prepareStatus)
                      ? "default"
                      : "destructive"
                  }
                >
                  Prepare status: {prepareStatus}
                </Badge>
              )}
              {runningActionId && (
                <Badge variant="secondary">Action running...</Badge>
              )}
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Load Inputs</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {loadInputs.length === 0 && (
                    <p className="text-sm text-muted-foreground">
                      No load inputs configured.
                    </p>
                  )}
                  {loadInputs.map((field) =>
                    renderRuntimeInputField(
                      field,
                      loadInputValues,
                      (fieldId, value) =>
                        setLoadInputValues((previousValues) => ({
                          ...previousValues,
                          [fieldId]: value,
                        })),
                      isPrepared ||
                        runtimeInteractionLocked ||
                        !canRunDashboardRuntime,
                      loadInputErrors[field.id]
                    )
                  )}
                  <Button
                    onClick={handlePrepareDashboard}
                    disabled={
                      runtimeInteractionLocked ||
                      !canRunDashboardRuntime ||
                      isPrepared
                    }
                    className="gap-2"
                  >
                    {isPreparing ? (
                      <>
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Loading...
                      </>
                    ) : (
                      <>
                        <Play className="h-4 w-4" />
                        Load Dashboard
                      </>
                    )}
                  </Button>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between">
                  <CardTitle className="text-base">Action Inputs</CardTitle>
                  <Button
                    variant="destructive"
                    size="sm"
                    className="gap-2"
                    onClick={() => setResetConfirmOpen(true)}
                    disabled={
                      runtimeInteractionLocked ||
                      !canRunDashboardRuntime ||
                      !isPrepared
                    }
                  >
                    <RotateCcw className="h-4 w-4" />
                    Reset
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  {actionInputs.length === 0 && (
                    <p className="text-sm text-muted-foreground">
                      No action inputs configured.
                    </p>
                  )}
                  {actionInputs.map((field) =>
                    renderRuntimeInputField(
                      field,
                      actionInputValues,
                      (fieldId, value) =>
                        setActionInputValues((previousValues) => ({
                          ...previousValues,
                          [fieldId]: value,
                        })),
                      !isPrepared ||
                        runtimeInteractionLocked ||
                        !canRunDashboardRuntime,
                      actionInputErrors[field.id]
                    )
                  )}
                  {!isPrepared && (
                    <p className="text-xs text-muted-foreground">
                      Action inputs unlock after you load the dashboard.
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>

            <div className="grid gap-4 lg:grid-cols-3">
              {DASHBOARD_SECTIONS.map((section) => (
                <Card
                  key={section}
                  className={
                    actionsBySection[section].length === 0
                      ? "invisible"
                      : undefined
                  }
                  aria-hidden={actionsBySection[section].length === 0}
                >
                  <CardHeader>
                    <CardTitle className="text-base capitalize">
                      {section} actions
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    {actionsBySection[section].length === 0 && (
                      <p className="text-sm text-muted-foreground">
                        No actions in this section.
                      </p>
                    )}
                    {actionsBySection[section].map((action) => {
                      const actionResult = actionResults[action.id];
                      return (
                        <div key={action.id} className="space-y-2">
                          <Button
                            className="w-full justify-start"
                            onClick={() => handleActionRun(action)}
                            disabled={
                              runtimeInteractionLocked ||
                              !canRunDashboardRuntime ||
                              !isPrepared ||
                              !action.appId
                            }
                          >
                            {runningActionId === action.id && (
                              <Loader2 className="h-4 w-4 animate-spin mr-2" />
                            )}
                            {action.label || "Run Action"}
                          </Button>
                          {actionResult && (
                            <div className="rounded-md border p-2 space-y-1">
                              <p className="text-xs">
                                Status:{" "}
                                <span
                                  className={
                                    isSuccessfulStatus(actionResult.status)
                                      ? "text-green-600"
                                      : "text-red-600"
                                  }
                                >
                                  {actionResult.status || "Unknown"}
                                </span>
                              </p>
                              {actionResult.errorMessage && (
                                <p className="text-xs text-red-600">
                                  {action.errorMessage ||
                                    actionResult.errorMessage}
                                </p>
                              )}
                              {actionResult.response && (
                                <pre className="text-xs max-h-40 overflow-auto bg-muted p-2 rounded">
                                  {formatResponse(actionResult.response)}
                                </pre>
                              )}
                              {isSuccessfulStatus(actionResult.status) &&
                                action.successMessage && (
                                  <p className="text-xs text-green-700">
                                    {action.successMessage}
                                  </p>
                                )}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </CardContent>
                </Card>
              ))}
            </div>

            {(runtimeError || prepareErrorMessage || prepareResponse) && (
              <div className="space-y-2">
                {runtimeError && (
                  <p className="text-sm text-red-600">{runtimeError}</p>
                )}
                {prepareErrorMessage && (
                  <p className="text-sm text-red-600">{prepareErrorMessage}</p>
                )}
                {prepareResponse && (
                  <pre className="text-xs max-h-56 overflow-auto bg-muted rounded p-3">
                    {formatResponse(prepareResponse)}
                  </pre>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {!isRunMode && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>Prepare App</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Label htmlFor="dashboard-prepare-app">
                App run during Load Dashboard
              </Label>
              <Select
                value={prepareAppId || "none"}
                onValueChange={(value) =>
                  setPrepareAppId(value === "none" ? "" : value)
                }
                disabled={!canEditDashboard}
              >
                <SelectTrigger id="dashboard-prepare-app" className="max-w-lg">
                  <SelectValue placeholder="Select app" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">No prepare app</SelectItem>
                  {availableApps.map((app) => (
                    <SelectItem key={app.id} value={app.id}>
                      {app.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </CardContent>
          </Card>

          <Separator />

          <Card>
            <CardHeader>
              <CardTitle>Load Inputs</CardTitle>
            </CardHeader>
            <CardContent>
              <InputFieldEditor
                fields={loadInputs}
                onChange={setLoadInputs}
                disabled={!canEditDashboard}
              />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Action Inputs</CardTitle>
            </CardHeader>
            <CardContent>
              <InputFieldEditor
                fields={actionInputs}
                onChange={setActionInputs}
                disabled={!canEditDashboard}
              />
            </CardContent>
          </Card>

          <Separator />

          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Actions</CardTitle>
              {canEditDashboard && (
                <Button size="sm" onClick={handleAddAction}>
                  <Plus className="h-4 w-4 mr-1" />
                  Add Action
                </Button>
              )}
            </CardHeader>
            <CardContent className="space-y-4">
              {actions.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  No actions configured. Add actions and map each to an app.
                </p>
              )}
              {actions.map((action) => (
                <Card key={action.id}>
                  <CardContent className="py-4 space-y-3">
                    <div className="grid gap-3 md:grid-cols-4">
                      <div className="space-y-1">
                        <Label>Action Label</Label>
                        <Input
                          value={action.label}
                          onChange={(event) =>
                            handleUpdateAction(action.id, {
                              label: event.target.value,
                            })
                          }
                          placeholder="Pass, Fail, Retry..."
                          disabled={!canEditDashboard}
                        />
                      </div>
                      <div className="space-y-1">
                        <Label>Target App</Label>
                        <Select
                          value={action.appId || "none"}
                          onValueChange={(value) =>
                            handleUpdateAction(action.id, {
                              appId: value === "none" ? "" : value,
                            })
                          }
                          disabled={!canEditDashboard}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select app" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="none">Select app</SelectItem>
                            {availableApps.map((app) => (
                              <SelectItem key={app.id} value={app.id}>
                                {app.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-1">
                        <Label>Section</Label>
                        <Select
                          value={action.section}
                          onValueChange={(value) =>
                            handleUpdateAction(action.id, {
                              section: value as DashboardSection,
                            })
                          }
                          disabled={!canEditDashboard}
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="left">Left</SelectItem>
                            <SelectItem value="center">Center</SelectItem>
                            <SelectItem value="right">Right</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="flex items-end justify-end">
                        {canEditDashboard && (
                          <Button
                            variant="destructive"
                            size="icon"
                            onClick={() => handleDeleteAction(action.id)}
                            aria-label="Delete action"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </div>

                    <div className="grid gap-3 md:grid-cols-2">
                      <div className="space-y-1">
                        <Label>Success Message</Label>
                        <Input
                          value={action.successMessage || ""}
                          onChange={(event) =>
                            handleUpdateAction(action.id, {
                              successMessage: event.target.value,
                            })
                          }
                          placeholder="Action completed"
                          disabled={!canEditDashboard}
                        />
                      </div>
                      <div className="space-y-1">
                        <Label>Error Message</Label>
                        <Input
                          value={action.errorMessage || ""}
                          onChange={(event) =>
                            handleUpdateAction(action.id, {
                              errorMessage: event.target.value,
                            })
                          }
                          placeholder="Action failed"
                          disabled={!canEditDashboard}
                        />
                      </div>
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center gap-2">
                        <Switch
                          checked={!!action.confirmEnabled}
                          onCheckedChange={(checked) =>
                            handleUpdateAction(action.id, {
                              confirmEnabled: checked,
                            })
                          }
                          disabled={!canEditDashboard}
                        />
                        <span className="text-sm text-muted-foreground">
                          Require confirmation before running
                        </span>
                      </div>
                      {action.confirmEnabled && (
                        <div className="grid gap-3 md:grid-cols-2">
                          <div className="space-y-1">
                            <Label>Confirm Title</Label>
                            <Input
                              value={action.confirmTitle || ""}
                              onChange={(event) =>
                                handleUpdateAction(action.id, {
                                  confirmTitle: event.target.value,
                                })
                              }
                              placeholder="Confirm Action"
                              disabled={!canEditDashboard}
                            />
                          </div>
                          <div className="space-y-1">
                            <Label>Confirm Message</Label>
                            <Input
                              value={action.confirmMessage || ""}
                              onChange={(event) =>
                                handleUpdateAction(action.id, {
                                  confirmMessage: event.target.value,
                                })
                              }
                              placeholder="Are you sure?"
                              disabled={!canEditDashboard}
                            />
                          </div>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Bindings</CardTitle>
              {canEditDashboard && (
                <Button
                  size="sm"
                  onClick={handleAddBinding}
                  disabled={actions.length === 0}
                >
                  <Plus className="h-4 w-4 mr-1" />
                  Add Binding
                </Button>
              )}
            </CardHeader>
            <CardContent className="space-y-4">
              {bindings.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  No bindings configured. Bind each action input to load/action
                  inputs, prepare output, or literals.
                </p>
              )}
              {bindings.map((binding) => (
                <Card key={binding.id}>
                  <CardContent className="py-4 space-y-3">
                    <div className="grid gap-3 md:grid-cols-4">
                      <div className="space-y-1">
                        <Label>Action</Label>
                        <Select
                          value={binding.actionId || "none"}
                          onValueChange={(value) =>
                            handleUpdateBinding(binding.id, {
                              actionId: value === "none" ? "" : value,
                            })
                          }
                          disabled={!canEditDashboard}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select action" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="none">Select action</SelectItem>
                            {actions.map((action) => (
                              <SelectItem key={action.id} value={action.id}>
                                {action.label || action.id}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-1">
                        <Label>Action Input Name</Label>
                        <Input
                          value={binding.inputName}
                          onChange={(event) =>
                            handleUpdateBinding(binding.id, {
                              inputName: event.target.value,
                            })
                          }
                          placeholder="inputName"
                          disabled={!canEditDashboard}
                        />
                      </div>
                      <div className="space-y-1">
                        <Label>Source</Label>
                        <Select
                          value={binding.sourceType}
                          onValueChange={(value) =>
                            handleUpdateBinding(binding.id, {
                              sourceType: value as DashboardBindingSourceType,
                              sourceKey:
                                value === "literal" ? "" : binding.sourceKey,
                              literalValue:
                                value === "literal" ? binding.literalValue : "",
                            })
                          }
                          disabled={!canEditDashboard}
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="load_input">
                              Load Input
                            </SelectItem>
                            <SelectItem value="action_input">
                              Action Input
                            </SelectItem>
                            <SelectItem value="prepare_output">
                              Prepare Output
                            </SelectItem>
                            <SelectItem value="literal">Literal</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="flex items-end justify-end">
                        {canEditDashboard && (
                          <Button
                            variant="destructive"
                            size="icon"
                            onClick={() => handleDeleteBinding(binding.id)}
                            aria-label="Delete binding"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </div>

                    {binding.sourceType === "literal" ? (
                      <div className="space-y-1">
                        <Label>Literal Value</Label>
                        <Input
                          value={binding.literalValue || ""}
                          onChange={(event) =>
                            handleUpdateBinding(binding.id, {
                              literalValue: event.target.value,
                            })
                          }
                          placeholder="literal value"
                          disabled={!canEditDashboard}
                        />
                      </div>
                    ) : (
                      <div className="space-y-1">
                        <Label>Source Key / Path</Label>
                        <Input
                          value={binding.sourceKey || ""}
                          onChange={(event) =>
                            handleUpdateBinding(binding.id, {
                              sourceKey: event.target.value,
                            })
                          }
                          placeholder={
                            binding.sourceType === "prepare_output"
                              ? "data.customer.id"
                              : "input title"
                          }
                          disabled={!canEditDashboard}
                        />
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))}
            </CardContent>
          </Card>
        </>
      )}

      {isRunMode && (
        <AlertDialog
          open={resetConfirmOpen}
          onOpenChange={(open) => {
            if (!runtimeInteractionLocked) {
              setResetConfirmOpen(open);
            }
          }}
        >
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle className="text-red-600">
                Reset Dashboard Runtime
              </AlertDialogTitle>
              <AlertDialogDescription>
                This clears loaded runtime state, prepare run, and action run
                results. This does not change saved dashboard configuration.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <Button
                variant="outline"
                onClick={() => setResetConfirmOpen(false)}
                disabled={runtimeInteractionLocked}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                onClick={resetRuntimeState}
                disabled={runtimeInteractionLocked}
              >
                Reset
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}

      {isRunMode && (
        <AlertDialog
          open={!!selectedConfirmAction}
          onOpenChange={(open) => {
            if (!open) {
              setConfirmActionId(null);
            }
          }}
        >
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>
                {selectedConfirmAction?.confirmTitle || "Confirm Action"}
              </AlertDialogTitle>
              <AlertDialogDescription>
                {selectedConfirmAction?.confirmMessage ||
                  "Are you sure you want to run this action?"}
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <Button
                variant="outline"
                onClick={() => setConfirmActionId(null)}
                disabled={runtimeInteractionLocked}
              >
                Cancel
              </Button>
              <Button
                onClick={async () => {
                  if (!selectedConfirmAction) {
                    return;
                  }
                  await executeAction(selectedConfirmAction);
                }}
                disabled={runtimeInteractionLocked}
              >
                Run Action
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}
    </section>
  );
}

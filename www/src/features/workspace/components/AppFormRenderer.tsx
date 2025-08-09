import { useMemo } from "react";
import { useForm, Controller } from "react-hook-form";
import { AppNode, FieldType, Endpoint } from "../types";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { toast } from "@/hooks/use-toast";

export default function AppFormRenderer({
  app,
  endpoint,
}: {
  app: AppNode;
  endpoint?: Endpoint;
}) {
  const defaultValues = useMemo(() => {
    const dv: Record<string, any> = {};
    app.fields.forEach((f) => {
      dv[f.id] = f.type === "boolean" ? false : "";
    });
    return dv;
  }, [app.fields]);

  const { handleSubmit, register, control, formState } = useForm({
    defaultValues,
  });

  const onSubmit = async (data: any) => {
    if (!endpoint) {
      toast({
        title: "No endpoint selected",
        description: "Please choose an endpoint in Build tab.",
      });
      return;
    }
    try {
      // Build URL with query params (endpoint.query + form values for GET-like methods)
      const urlObj = new URL(endpoint.url);
      (endpoint.query || []).forEach(({ key, value }) => {
        if (key) urlObj.searchParams.set(key, value);
      });

      const method = endpoint.method || "POST";
      const isGetLike = ["GET", "HEAD", "OPTIONS"].includes(method);

      if (isGetLike) {
        Object.entries(data || {}).forEach(([k, v]) => {
          if (v !== undefined && v !== null)
            urlObj.searchParams.set(k, String(v));
        });
      }

      const headers = new Headers();
      (endpoint.headers || []).forEach(({ key, value }) => {
        if (key) headers.set(key, value);
      });

      let body: BodyInit | undefined = undefined;
      if (!isGetLike) {
        const staticBody: Record<string, any> = {};
        (endpoint.body || []).forEach(({ key, value }) => {
          if (key) staticBody[key] = value;
        });
        body = JSON.stringify({ ...data, ...staticBody });
        if (!headers.has("Content-Type"))
          headers.set("Content-Type", "application/json");
      }

      const res = await fetch(urlObj.toString(), { method, headers, body });
      const ok = res.ok;
      const text = await res.text();
      if (ok) {
        toast({
          title: "Submitted",
          description: `Endpoint ${method} ${urlObj.pathname} returned ${res.status}.`,
        });
        console.log("Endpoint response:", text);
      } else {
        toast({
          title: "Error",
          description: `Request failed with ${res.status}. See console for details.`,
        });
        console.error("Endpoint error response:", text);
      }
    } catch (err) {
      toast({
        title: "Error",
        description: "Failed to call endpoint. Check console.",
      });
      console.error(err);
    }
  };

  return (
    <form className="max-w-2xl space-y-4" onSubmit={handleSubmit(onSubmit)}>
      {app.fields.map((f) => (
        <div key={f.id} className="grid gap-2">
          <label className="text-sm font-medium">
            {f.label}
            {f.required ? " *" : ""}
          </label>
          {renderField(f.type, f.id, register, control, !!f.required)}
          {formState.errors?.[f.id] && (
            <p className="text-sm text-destructive">This field is required</p>
          )}
        </div>
      ))}
      <Button type="submit" disabled={!endpoint}>
        Submit
      </Button>
    </form>
  );
}

function renderField(
  type: FieldType,
  id: string,
  register: any,
  control: any,
  required: boolean,
) {
  switch (type) {
    case "text":
      return (
        <Input {...register(id, required ? { required: true } : undefined)} />
      );
    case "email":
      return (
        <Input
          type="email"
          {...register(id, required ? { required: true } : undefined)}
        />
      );
    case "date":
      return (
        <Input
          type="date"
          {...register(id, required ? { required: true } : undefined)}
        />
      );
    case "integer":
      return (
        <Input
          type="number"
          step={1}
          {...register(id, {
            valueAsNumber: true,
            ...(required ? { required: true } : {}),
          })}
        />
      );
    case "boolean":
      return (
        <Controller
          control={control}
          name={id}
          render={({ field }) => (
            <div className="flex items-center gap-2">
              <Switch
                checked={!!field.value}
                onCheckedChange={field.onChange}
              />
            </div>
          )}
        />
      );
  }
}

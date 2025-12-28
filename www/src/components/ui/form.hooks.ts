import * as React from "react";
import { useFormContext } from "react-hook-form";

type FormFieldContextValue<
  TFieldValues extends import("react-hook-form").FieldValues = import("react-hook-form").FieldValues,
  TName extends import("react-hook-form").FieldPath<TFieldValues> = import("react-hook-form").FieldPath<TFieldValues>,
> = {
  name: TName;
};

export const FormFieldContext = React.createContext<FormFieldContextValue>(
  {} as FormFieldContextValue,
);

type FormItemContextValue = {
  id: string;
};

export const FormItemContext = React.createContext<FormItemContextValue>(
  {} as FormItemContextValue,
);

/**
 * Hook to access form field state and IDs within a form field component.
 * Must be used within a FormField component.
 */
export const useFormField = () => {
  const fieldContext = React.useContext(FormFieldContext);
  const itemContext = React.useContext(FormItemContext);
  const { getFieldState, formState } = useFormContext();

  const fieldState = getFieldState(fieldContext.name, formState);

  if (!fieldContext) {
    throw new Error("useFormField should be used within <FormField>");
  }

  const { id } = itemContext;

  return {
    id,
    name: fieldContext.name,
    formItemId: `${id}-form-item`,
    formDescriptionId: `${id}-form-item-description`,
    formMessageId: `${id}-form-item-message`,
    ...fieldState,
  };
};

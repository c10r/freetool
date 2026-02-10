import { describe, expect, it } from "vitest";
import type { EvaluationContext } from "../expression-evaluator";
import {
  evaluateExpression,
  extractExpressions,
  extractVariables,
  hasExpressions,
  processTemplate,
  validateExpression,
} from "../expression-evaluator";

const defaultContext: EvaluationContext = {
  variables: {},
  types: {},
  currentUser: {
    email: "test@example.com",
    id: "user-123",
    firstName: "Test",
    lastName: "User",
  },
};

describe("extractVariables", () => {
  it("should extract simple variable names", () => {
    expect(extractVariables("@Amount")).toEqual(["Amount"]);
  });

  it("should extract multiple variables", () => {
    expect(extractVariables("@Amount + @Price")).toEqual(["Amount", "Price"]);
  });

  it("should extract current_user variables", () => {
    expect(extractVariables("@current_user.email")).toEqual([
      "current_user.email",
    ]);
  });

  it("should not duplicate variables", () => {
    expect(extractVariables("@Amount + @Amount")).toEqual(["Amount"]);
  });

  it("should handle expressions without variables", () => {
    expect(extractVariables("1 + 2")).toEqual([]);
  });

  it("should extract quoted variable names with spaces", () => {
    expect(extractVariables('@"Customer ID"')).toEqual(["Customer ID"]);
  });

  it("should extract multiple quoted variables with spaces", () => {
    expect(extractVariables('@"Customer ID" + @"Order Total"')).toEqual([
      "Customer ID",
      "Order Total",
    ]);
  });

  it("should extract mixed quoted and unquoted variables", () => {
    expect(extractVariables('@"Customer ID" + @Amount')).toEqual([
      "Customer ID",
      "Amount",
    ]);
  });
});

describe("validateExpression", () => {
  it("should validate expression with known variables", () => {
    const result = validateExpression("@Amount * 2", ["Amount"]);
    expect(result.isValid).toBe(true);
    expect(result.errors).toHaveLength(0);
    expect(result.referencedVariables).toEqual(["Amount"]);
  });

  it("should report unknown variables", () => {
    const result = validateExpression("@Unknown + 1", ["Amount"]);
    expect(result.isValid).toBe(false);
    expect(result.errors).toContain("Unknown variable: Unknown");
  });

  it("should validate current_user properties", () => {
    const result = validateExpression("@current_user.email", []);
    expect(result.isValid).toBe(true);
  });

  it("should report invalid current_user properties", () => {
    const result = validateExpression("@current_user.invalid", []);
    expect(result.isValid).toBe(false);
    expect(result.errors).toContain("Unknown current_user property: invalid");
  });

  it("should report syntax errors", () => {
    const result = validateExpression("@Amount +", ["Amount"]);
    expect(result.isValid).toBe(false);
    expect(result.errors.some((e) => e.includes("Syntax error"))).toBe(true);
  });

  it("should validate quoted variables with spaces", () => {
    const result = validateExpression('@"Customer ID" * 2', ["Customer ID"]);
    expect(result.isValid).toBe(true);
    expect(result.errors).toHaveLength(0);
    expect(result.referencedVariables).toEqual(["Customer ID"]);
  });

  it("should report unknown quoted variables", () => {
    const result = validateExpression('@"Unknown Var" + 1', ["Amount"]);
    expect(result.isValid).toBe(false);
    expect(result.errors).toContain("Unknown variable: Unknown Var");
  });

  it("should validate JSON object expressions", () => {
    const result = validateExpression(
      '{ "amount": @Amount, "user": { "email": @current_user.email } }',
      ["Amount"]
    );
    expect(result.isValid).toBe(true);
  });

  it("should reject variables inside JSON strings", () => {
    const result = validateExpression('{ "name": "Hello @Name" }', ["Name"]);
    expect(result.isValid).toBe(false);
    expect(result.errors).toContain(
      "Variables cannot be used inside JSON strings; use @Var as a JSON value."
    );
  });
});

describe("evaluateExpression", () => {
  it("should evaluate arithmetic expressions", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "100" },
      types: { Amount: "integer" },
    };
    const result = evaluateExpression("@Amount * 2", context);
    expect(result).toEqual({ success: true, value: "200" });
  });

  it("should evaluate ternary expressions with boolean", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Debit: "true", Amount: "50" },
      types: { Debit: "boolean", Amount: "integer" },
    };
    const result = evaluateExpression(
      "@Debit ? -1 * @Amount : @Amount",
      context
    );
    expect(result).toEqual({ success: true, value: "-50" });
  });

  it("should evaluate ternary expressions with false boolean", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Debit: "false", Amount: "50" },
      types: { Debit: "boolean", Amount: "integer" },
    };
    const result = evaluateExpression(
      "@Debit ? -1 * @Amount : @Amount",
      context
    );
    expect(result).toEqual({ success: true, value: "50" });
  });

  it("should evaluate comparison expressions", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "100" },
      types: { Amount: "integer" },
    };
    const result = evaluateExpression("@Amount > 50", context);
    expect(result).toEqual({ success: true, value: "true" });
  });

  it("should evaluate logical expressions", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { A: "true", B: "false" },
      types: { A: "boolean", B: "boolean" },
    };
    const result = evaluateExpression("@A && !@B", context);
    expect(result).toEqual({ success: true, value: "true" });
  });

  it("should handle string comparison", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Status: "active" },
      types: { Status: "text" },
    };
    // String equality comparison works
    const result = evaluateExpression('@Status == "active"', context);
    expect(result).toEqual({ success: true, value: "true" });
  });

  it("should handle ternary with string result", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { IsVIP: "true" },
      types: { IsVIP: "boolean" },
    };
    const result = evaluateExpression(
      '@IsVIP ? "Premium" : "Standard"',
      context
    );
    expect(result).toEqual({ success: true, value: "Premium" });
  });

  it("should evaluate ternary with string input comparison using strict equality", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Cancel: "Immediately" },
      types: { Cancel: "text" },
    };
    const result = evaluateExpression(
      '@Cancel === "Immediately" ? true : false',
      context
    );
    expect(result).toEqual({ success: true, value: "true" });
  });

  it("should evaluate ternary with string input comparison to false branch", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Cancel: "Later" },
      types: { Cancel: "text" },
    };
    const result = evaluateExpression(
      '@Cancel === "Immediately" ? true : false',
      context
    );
    expect(result).toEqual({ success: true, value: "false" });
  });

  it("should treat quoted @variable as a string literal, not a variable", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Cancel: "Immediately" },
      types: { Cancel: "text" },
    };
    const result = evaluateExpression(
      "'@Cancel' === 'Immediately' ? true : false",
      context
    );
    expect(result).toEqual({ success: true, value: "false" });
  });

  it("should access current_user properties", () => {
    const result = evaluateExpression("@current_user.email", defaultContext);
    expect(result).toEqual({ success: true, value: "test@example.com" });
  });

  it("should return error for missing variable value", () => {
    const result = evaluateExpression("@Missing", defaultContext);
    expect(result.success).toBe(false);
    expect(result).toHaveProperty("error");
    if (!result.success) {
      expect(result.error).toContain("Missing");
    }
  });

  it("should coerce integer types correctly", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "42" },
      types: { Amount: "integer" },
    };
    const result = evaluateExpression("@Amount + 8", context);
    expect(result).toEqual({ success: true, value: "50" });
  });

  it("should handle division", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "100" },
      types: { Amount: "integer" },
    };
    const result = evaluateExpression("@Amount / 4", context);
    expect(result).toEqual({ success: true, value: "25" });
  });

  it("should handle modulo", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "17" },
      types: { Amount: "integer" },
    };
    const result = evaluateExpression("@Amount % 5", context);
    expect(result).toEqual({ success: true, value: "2" });
  });

  it("should handle parentheses", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { A: "10", B: "2" },
      types: { A: "integer", B: "integer" },
    };
    const result = evaluateExpression("(@A + @B) * 3", context);
    expect(result).toEqual({ success: true, value: "36" });
  });

  it("should evaluate quoted variables with spaces", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { "Customer ID": "42" },
      types: { "Customer ID": "integer" },
    };
    const result = evaluateExpression('@"Customer ID" * 2', context);
    expect(result).toEqual({ success: true, value: "84" });
  });

  it("should evaluate mixed quoted and unquoted variables", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { "Order Total": "100", Discount: "10" },
      types: { "Order Total": "integer", Discount: "integer" },
    };
    const result = evaluateExpression('@"Order Total" - @Discount', context);
    expect(result).toEqual({ success: true, value: "90" });
  });

  it("should evaluate complex expression with spaced variables", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { "Unit Price": "50", Quantity: "3", "Tax Rate": "10" },
      types: {
        "Unit Price": "integer",
        Quantity: "integer",
        "Tax Rate": "integer",
      },
    };
    const result = evaluateExpression(
      '(@"Unit Price" * @Quantity) * (1 + @"Tax Rate" / 100)',
      context
    );
    expect(result).toEqual({ success: true, value: "165" });
  });

  it("should evaluate JSON object expressions", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "42", Name: "Alice" },
      types: { Amount: "integer", Name: "text" },
    };
    const result = evaluateExpression(
      '{ "amount": @Amount, "name": @Name }',
      context
    );
    expect(result).toEqual({
      success: true,
      value: '{"amount":42,"name":"Alice"}',
    });
  });

  it("should evaluate JSON array expressions", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { A: "true", B: "5" },
      types: { A: "boolean", B: "integer" },
    };
    const result = evaluateExpression("[@A, @B, @current_user.email]", context);
    expect(result).toEqual({
      success: true,
      value: '[true,5,"test@example.com"]',
    });
  });
});

describe("processTemplate", () => {
  it("should process templates with expressions", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "100" },
      types: { Amount: "integer" },
    };
    const result = processTemplate("Total: {{ @Amount * 2 }} dollars", context);
    expect(result).toEqual({ success: true, value: "Total: 200 dollars" });
  });

  it("should process multiple expressions in template", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Price: "10", Qty: "5" },
      types: { Price: "integer", Qty: "integer" },
    };
    const result = processTemplate(
      "Price: {{ @Price }}, Qty: {{ @Qty }}, Total: {{ @Price * @Qty }}",
      context
    );
    expect(result).toEqual({
      success: true,
      value: "Price: 10, Qty: 5, Total: 50",
    });
  });

  it("should return error when expression evaluation fails", () => {
    const result = processTemplate("Value: {{ @Missing }}", defaultContext);
    expect(result.success).toBe(false);
  });

  it("should leave template unchanged when no expressions", () => {
    const result = processTemplate("No expressions here", defaultContext);
    expect(result).toEqual({ success: true, value: "No expressions here" });
  });

  it("should handle the Debit/Amount use case", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Debit: "true", Amount: "100" },
      types: { Debit: "boolean", Amount: "integer" },
    };
    const result = processTemplate(
      "amount={{ @Debit ? -1 * @Amount : @Amount }}",
      context
    );
    expect(result).toEqual({ success: true, value: "amount=-100" });
  });

  it("should process templates with quoted variables with spaces", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { "Customer ID": "12345" },
      types: { "Customer ID": "text" },
    };
    const result = processTemplate('Customer: {{ @"Customer ID" }}', context);
    expect(result).toEqual({ success: true, value: "Customer: 12345" });
  });

  it("should process templates with JSON expression output", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Amount: "10" },
      types: { Amount: "integer" },
    };
    const result = processTemplate(
      'payload={{ { "amount": @Amount } }}',
      context
    );
    expect(result).toEqual({
      success: true,
      value: 'payload={"amount":10}',
    });
  });

  it("should process template expression for cancel flag true case", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Cancel: "Immediately" },
      types: { Cancel: "text" },
    };
    const result = processTemplate(
      '{ "shouldCancel": {{ @Cancel === "Immediately" ? true : false }} }',
      context
    );
    expect(result).toEqual({
      success: true,
      value: '{ "shouldCancel": true }',
    });
  });

  it("should process template expression for cancel flag false case", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Cancel: "Later" },
      types: { Cancel: "text" },
    };
    const result = processTemplate(
      '{ "shouldCancel": {{ @Cancel === "Immediately" ? true : false }} }',
      context
    );
    expect(result).toEqual({
      success: true,
      value: '{ "shouldCancel": false }',
    });
  });

  it("should process template with quoted @Cancel literal as false", () => {
    const context: EvaluationContext = {
      ...defaultContext,
      variables: { Cancel: "Immediately" },
      types: { Cancel: "text" },
    };
    const result = processTemplate(
      "{ shouldCancel: {{ '@Cancel' === 'Immediately' ? true : false }} }",
      context
    );
    expect(result).toEqual({
      success: true,
      value: "{ shouldCancel: false }",
    });
  });
});

describe("hasExpressions", () => {
  it("should return true for templates with expressions", () => {
    expect(hasExpressions("{{ @Amount }}")).toBe(true);
    expect(hasExpressions("Total: {{ @Amount * 2 }}")).toBe(true);
  });

  it("should return false for templates without expressions", () => {
    expect(hasExpressions("No expressions")).toBe(false);
    expect(hasExpressions("{simple}")).toBe(false);
  });
});

describe("extractExpressions", () => {
  it("should extract all expressions from template", () => {
    const expressions = extractExpressions(
      "A: {{ @A }}, B: {{ @B + 1 }}, C: {{ @A + @B }}"
    );
    expect(expressions).toEqual(["@A", "@B + 1", "@A + @B"]);
  });

  it("should return empty array when no expressions", () => {
    expect(extractExpressions("No expressions")).toEqual([]);
  });
});

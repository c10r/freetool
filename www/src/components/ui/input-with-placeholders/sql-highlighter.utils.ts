type SqlTokenType = "plain" | "keyword" | "string" | "number" | "comment";

export interface SqlToken {
  type: SqlTokenType;
  value: string;
}

const KEYWORDS = new Set(
  [
    "SELECT",
    "FROM",
    "WHERE",
    "WITH",
    "AS",
    "COUNT",
    "CASE",
    "WHEN",
    "THEN",
    "ELSE",
    "END",
    "JOIN",
    "LEFT",
    "RIGHT",
    "FULL",
    "OUTER",
    "INNER",
    "CROSS",
    "ON",
    "GROUP",
    "BY",
    "ORDER",
    "LIMIT",
    "OFFSET",
    "AND",
    "OR",
    "NOT",
    "NULL",
    "IS",
    "IN",
    "LIKE",
    "ILIKE",
    "DISTINCT",
    "UNION",
    "ALL",
    "HAVING",
    "EXISTS",
    "BETWEEN",
    "CREATE",
    "TABLE",
    "INSERT",
    "UPDATE",
    "DELETE",
    "INTO",
    "VALUES",
    "SET",
    "TRUE",
    "FALSE",
    "ASC",
    "DESC",
  ].map((keyword) => keyword.toUpperCase())
);

const isWordStart = (value: string): boolean => /[A-Za-z_]/.test(value);
const isWordChar = (value: string): boolean => /[A-Za-z0-9_$]/.test(value);

export function tokenizeSqlText(text: string): SqlToken[] {
  const tokens: SqlToken[] = [];
  let i = 0;

  while (i < text.length) {
    const char = text[i];
    const nextChar = text[i + 1];

    if (char === "-" && nextChar === "-") {
      tokens.push({ type: "comment", value: text.slice(i) });
      break;
    }

    if (char === "/" && nextChar === "*") {
      const endIndex = text.indexOf("*/", i + 2);
      const end = endIndex === -1 ? text.length : endIndex + 2;
      tokens.push({ type: "comment", value: text.slice(i, end) });
      i = end;
      continue;
    }

    if (char === "'" || char === '"') {
      const quote = char;
      let j = i + 1;
      while (j < text.length) {
        if (text[j] === quote) {
          if (quote === "'" && text[j + 1] === "'") {
            j += 2;
            continue;
          }
          j += 1;
          break;
        }
        j += 1;
      }
      tokens.push({ type: "string", value: text.slice(i, j) });
      i = j;
      continue;
    }

    if (/\s/.test(char)) {
      let j = i + 1;
      while (j < text.length && /\s/.test(text[j])) {
        j += 1;
      }
      tokens.push({ type: "plain", value: text.slice(i, j) });
      i = j;
      continue;
    }

    if (/[0-9]/.test(char)) {
      let j = i + 1;
      while (j < text.length && /[0-9._]/.test(text[j])) {
        j += 1;
      }
      tokens.push({ type: "number", value: text.slice(i, j) });
      i = j;
      continue;
    }

    if (isWordStart(char)) {
      let j = i + 1;
      while (j < text.length && isWordChar(text[j])) {
        j += 1;
      }
      const word = text.slice(i, j);
      const upper = word.toUpperCase();
      const type = KEYWORDS.has(upper) ? "keyword" : "plain";
      tokens.push({ type, value: word });
      i = j;
      continue;
    }

    tokens.push({ type: "plain", value: char });
    i += 1;
  }

  return tokens;
}

export function getSqlTokenStyle(type: SqlTokenType): string | null {
  switch (type) {
    case "keyword":
      return "color: hsl(var(--sql-token-keyword));";
    case "string":
      return "color: hsl(var(--sql-token-string));";
    case "number":
      return "color: hsl(var(--sql-token-number));";
    case "comment":
      return "color: hsl(var(--sql-token-comment));";
    default:
      return null;
  }
}

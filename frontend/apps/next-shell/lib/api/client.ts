/**
 * US-077: Type-safe API client using openapi-fetch + generated types.
 *
 * All API calls are fully typed based on the OpenAPI schema.
 * Use this client instead of raw fetch() or apiFetch() for type safety.
 */

import createClient from "openapi-fetch";
import type { paths } from "./generated";

export const apiClient = createClient<paths>({
  baseUrl: typeof window !== "undefined" ? "" : (process.env.IMS_API_URL ?? "http://localhost:8080"),
  credentials: "include",
});

// Re-export component types for convenience
export type { components, operations } from "./generated";

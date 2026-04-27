import { describe, it, expect } from "vitest";
import type { components, operations } from "@/lib/api/generated";

/**
 * US-077: Type-safety tests for generated API types.
 * These tests verify that the generated types have the expected shape.
 */
describe("Generated API Types", () => {
  it("LoginResponse has required fields", () => {
    const response: components["schemas"]["LoginResponse"] = {
      accessToken: "token",
      refreshToken: "refresh",
      expiresAt: "2026-04-27T00:00:00Z",
      username: "admin",
      email: "admin@ims.com",
      roles: ["Admin"],
    };
    expect(response.accessToken).toBe("token");
    expect(response.roles).toContain("Admin");
  });

  it("IssueDto status is a valid union type", () => {
    const issue: components["schemas"]["IssueDto"] = {
      id: "00000000-0000-0000-0000-000000000001",
      title: "Test Issue",
      status: "Open",
      priority: "Medium",
      reporterId: "00000000-0000-0000-0000-000000000002",
      createdAt: "2026-04-27T00:00:00Z",
    };
    expect(["Open", "InProgress", "Resolved", "Closed"]).toContain(issue.status);
  });

  it("ProductDto has required inventory fields", () => {
    const product: components["schemas"]["ProductDto"] = {
      id: "00000000-0000-0000-0000-000000000003",
      name: "Laptop Dell",
      sku: "DELL-001",
      quantityInStock: 10,
      minimumStockLevel: 5,
      unitPrice: 1500.0,
      createdAt: "2026-04-27T00:00:00Z",
    };
    expect(product.quantityInStock).toBe(10);
    expect(product.sku).toBe("DELL-001");
  });

  it("SearchResponse has results array and total", () => {
    const search: components["schemas"]["SearchResponse"] = {
      results: [
        { module: "issues", type: "issue", id: "1", title: "Bug", score: 0.9 },
      ],
      total: 1,
    };
    expect(search.results).toHaveLength(1);
    expect(search.total).toBe(1);
  });

  it("CreateIssueRequest enforces priority enum", () => {
    const req: components["schemas"]["CreateIssueRequest"] = {
      title: "New Issue",
      priority: "High",
    };
    expect(["Low", "Medium", "High", "Critical"]).toContain(req.priority);
  });
});

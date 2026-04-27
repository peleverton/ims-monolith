/**
 * US-077: OpenAPI-generated types for the IMS API.
 *
 * This file is AUTO-GENERATED. Do not edit manually.
 * Regenerate by running: npm run generate:api
 *
 * Source: http://localhost:8080/swagger/v1/swagger.json
 */

export interface paths {
  "/api/auth/login": {
    post: operations["Login"];
  };
  "/api/auth/refresh": {
    post: operations["RefreshToken"];
  };
  "/api/auth/logout": {
    post: operations["Logout"];
  };
  "/api/issues": {
    get: operations["GetIssues"];
    post: operations["CreateIssue"];
  };
  "/api/issues/{id}": {
    get: operations["GetIssue"];
    put: operations["UpdateIssue"];
    delete: operations["DeleteIssue"];
  };
  "/api/issues/{id}/status": {
    patch: operations["UpdateIssueStatus"];
  };
  "/api/inventory/products": {
    get: operations["GetProducts"];
    post: operations["CreateProduct"];
  };
  "/api/inventory/products/{id}": {
    get: operations["GetProduct"];
    put: operations["UpdateProduct"];
    delete: operations["DeleteProduct"];
  };
  "/api/search": {
    get: operations["Search"];
  };
  "/api/features": {
    get: operations["GetFeatureFlags"];
  };
  "/api/health": {
    get: operations["GetHealth"];
  };
}

export interface components {
  schemas: {
    LoginRequest: {
      username: string;
      password: string;
    };
    LoginResponse: {
      accessToken: string;
      refreshToken: string;
      expiresAt: string;
      username: string;
      email: string;
      roles: string[];
    };
    IssueDto: {
      id: string;
      title: string;
      description?: string | null;
      status: "Open" | "InProgress" | "Resolved" | "Closed";
      priority: "Low" | "Medium" | "High" | "Critical";
      assigneeId?: string | null;
      reporterId: string;
      dueDate?: string | null;
      resolvedAt?: string | null;
      createdAt: string;
      updatedAt?: string | null;
    };
    CreateIssueRequest: {
      title: string;
      description?: string | null;
      priority: "Low" | "Medium" | "High" | "Critical";
      assigneeId?: string | null;
      dueDate?: string | null;
    };
    PagedResult: {
      items: unknown[];
      totalCount: number;
      page: number;
      pageSize: number;
    };
    ProductDto: {
      id: string;
      name: string;
      sku: string;
      description?: string | null;
      quantityInStock: number;
      minimumStockLevel: number;
      unitPrice: number;
      category?: string | null;
      supplierId?: string | null;
      locationId?: string | null;
      createdAt: string;
      updatedAt?: string | null;
    };
    SearchResultItem: {
      module: string;
      type: string;
      id: string;
      title: string;
      description?: string | null;
      score: number;
    };
    SearchResponse: {
      results: components["schemas"]["SearchResultItem"][];
      total: number;
    };
    ProblemDetails: {
      type?: string | null;
      title?: string | null;
      status?: number | null;
      detail?: string | null;
      instance?: string | null;
    };
  };
}

export interface operations {
  Login: {
    requestBody: { content: { "application/json": components["schemas"]["LoginRequest"] } };
    responses: {
      200: { content: { "application/json": components["schemas"]["LoginResponse"] } };
      400: { content: { "application/json": components["schemas"]["ProblemDetails"] } };
      401: { content: { "application/json": components["schemas"]["ProblemDetails"] } };
    };
  };
  RefreshToken: {
    responses: {
      200: { content: { "application/json": components["schemas"]["LoginResponse"] } };
      401: { content: { "application/json": components["schemas"]["ProblemDetails"] } };
    };
  };
  Logout: {
    responses: { 200: { content: { "application/json": Record<string, never> } } };
  };
  GetIssues: {
    parameters: {
      query?: {
        status?: string;
        priority?: string;
        assigneeId?: string;
        page?: number;
        pageSize?: number;
        sortBy?: string;
        sortDirection?: string;
      };
    };
    responses: {
      200: {
        content: {
          "application/json": components["schemas"]["PagedResult"] & {
            items: components["schemas"]["IssueDto"][];
          };
        };
      };
    };
  };
  CreateIssue: {
    requestBody: { content: { "application/json": components["schemas"]["CreateIssueRequest"] } };
    responses: {
      201: { content: { "application/json": components["schemas"]["IssueDto"] } };
      400: { content: { "application/json": components["schemas"]["ProblemDetails"] } };
    };
  };
  GetIssue: {
    parameters: { path: { id: string } };
    responses: {
      200: { content: { "application/json": components["schemas"]["IssueDto"] } };
      404: { content: { "application/json": components["schemas"]["ProblemDetails"] } };
    };
  };
  UpdateIssue: {
    parameters: { path: { id: string } };
    requestBody: { content: { "application/json": components["schemas"]["CreateIssueRequest"] } };
    responses: {
      200: { content: { "application/json": components["schemas"]["IssueDto"] } };
    };
  };
  DeleteIssue: {
    parameters: { path: { id: string } };
    responses: { 204: never };
  };
  UpdateIssueStatus: {
    parameters: { path: { id: string } };
    requestBody: { content: { "application/json": { status: string } } };
    responses: { 200: { content: { "application/json": components["schemas"]["IssueDto"] } } };
  };
  GetProducts: {
    parameters: {
      query?: { search?: string; category?: string; page?: number; pageSize?: number };
    };
    responses: {
      200: {
        content: {
          "application/json": components["schemas"]["PagedResult"] & {
            items: components["schemas"]["ProductDto"][];
          };
        };
      };
    };
  };
  CreateProduct: {
    requestBody: { content: { "application/json": components["schemas"]["ProductDto"] } };
    responses: {
      201: { content: { "application/json": components["schemas"]["ProductDto"] } };
    };
  };
  GetProduct: {
    parameters: { path: { id: string } };
    responses: {
      200: { content: { "application/json": components["schemas"]["ProductDto"] } };
      404: { content: { "application/json": components["schemas"]["ProblemDetails"] } };
    };
  };
  UpdateProduct: {
    parameters: { path: { id: string } };
    requestBody: { content: { "application/json": components["schemas"]["ProductDto"] } };
    responses: { 200: { content: { "application/json": components["schemas"]["ProductDto"] } } };
  };
  DeleteProduct: {
    parameters: { path: { id: string } };
    responses: { 204: never };
  };
  Search: {
    parameters: {
      query: { q: string; modules?: string; page?: number; pageSize?: number };
    };
    responses: {
      200: { content: { "application/json": components["schemas"]["SearchResponse"] } };
    };
  };
  GetFeatureFlags: {
    responses: {
      200: { content: { "application/json": Record<string, boolean> } };
    };
  };
  GetHealth: {
    responses: { 200: { content: { "application/json": unknown } } };
  };
}

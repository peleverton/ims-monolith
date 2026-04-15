// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  username: string;
  email: string;
  roles: string[];
}

// ─── Issues ───────────────────────────────────────────────────────────────────

export type IssueStatus = "Open" | "InProgress" | "Resolved" | "Closed";
export type IssuePriority = "Low" | "Medium" | "High" | "Critical";

export interface IssueDto {
  id: string;
  title: string;
  description: string;
  status: IssueStatus;
  priority: IssuePriority;
  assigneeId?: string;
  assigneeName?: string;
  createdAt: string;
  updatedAt: string;
  tags: string[];
  commentsCount: number;
}

export interface CreateIssueRequest {
  title: string;
  description: string;
  priority: IssuePriority;
  assigneeId?: string;
  tags?: string[];
}

export interface UpdateIssueRequest {
  title?: string;
  description?: string;
  priority?: IssuePriority;
}

// ─── Inventory ────────────────────────────────────────────────────────────────

export interface InventoryItemDto {
  id: string;
  name: string;
  sku: string;
  quantity: number;
  location: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

// ─── Pagination ───────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// ─── Analytics ────────────────────────────────────────────────────────────────

export interface AnalyticsSummaryDto {
  totalIssues: number;
  openIssues: number;
  resolvedIssues: number;
  closedIssues: number;
  totalInventoryItems: number;
  issuesByStatus: Record<IssueStatus, number>;
  issuesByPriority: Record<IssuePriority, number>;
  issuesByDay: { date: string; count: number }[];
}

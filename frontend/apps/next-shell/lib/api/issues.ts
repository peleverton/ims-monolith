/**
 * lib/api/issues.ts — US-068
 *
 * Client-side API functions for the Issues module.
 * All calls go through the BFF proxy: /api/proxy/issues/*
 */

import { apiFetch } from "@/lib/api-client";
import type {
  IssueDto,
  CreateIssueRequest,
  UpdateIssueRequest,
  IssueStatus,
  IssuePriority,
  PagedResult,
} from "@/lib/types";

const BASE = "/api/proxy/issues";

// ── Query params ─────────────────────────────────────────────────────────────

export interface GetIssuesParams {
  page?: number;
  pageSize?: number;
  status?: IssueStatus | "";
  priority?: IssuePriority | "";
  search?: string;
}

// ── Read ─────────────────────────────────────────────────────────────────────

export function getIssues(params: GetIssuesParams = {}) {
  const qs = new URLSearchParams();
  qs.set("pageNumber", String(params.page ?? 1));
  qs.set("pageSize", String(params.pageSize ?? 15));
  if (params.status)   qs.set("status", params.status);
  if (params.priority) qs.set("priority", params.priority);
  if (params.search)   qs.set("searchTerm", params.search);
  return apiFetch<PagedResult<IssueDto>>(`${BASE}?${qs}`);
}

export function getIssueById(id: string) {
  return apiFetch<IssueDto>(`${BASE}/${id}`);
}

// ── Write ─────────────────────────────────────────────────────────────────────

export function createIssue(data: CreateIssueRequest) {
  return apiFetch<IssueDto>(BASE, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export function updateIssue(id: string, data: UpdateIssueRequest) {
  return apiFetch<IssueDto>(`${BASE}/${id}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export function updateIssueStatus(id: string, status: IssueStatus) {
  return apiFetch<IssueDto>(`${BASE}/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status }),
  });
}

export function assignIssue(id: string, assigneeId: string) {
  return apiFetch<IssueDto>(`${BASE}/${id}/assign`, {
    method: "PATCH",
    body: JSON.stringify({ assigneeId }),
  });
}

export function addComment(issueId: string, content: string) {
  return apiFetch<IssueDto>(`${BASE}/${issueId}/comments`, {
    method: "POST",
    body: JSON.stringify({ content }),
  });
}

export function deleteIssue(id: string) {
  return apiFetch<void>(`${BASE}/${id}`, {
    method: "DELETE",
  });
}

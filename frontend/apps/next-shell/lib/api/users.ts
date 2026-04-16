/**
 * lib/api/users.ts — US-040
 *
 * Funções client-side para o módulo de admin de usuários.
 * Usa apiFetch (api-client.ts) com interceptor de refresh token.
 * Todas as chamadas passam pelo BFF proxy: /api/proxy/admin/users
 */

import { apiFetch } from "@/lib/api-client";
import type {
  UserAdminDto,
  RoleDto,
  InviteUserRequest,
  PagedResult,
} from "@/lib/types";

const BASE = "/api/proxy/admin/users";

export interface GetUsersParams {
  page?: number;
  pageSize?: number;
  search?: string;
  role?: string;
}

export function getAdminUsers(params: GetUsersParams = {}) {
  const qs = new URLSearchParams();
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  if (params.search) qs.set("search", params.search);
  if (params.role) qs.set("role", params.role);
  return apiFetch<PagedResult<UserAdminDto>>(`${BASE}?${qs}`);
}

export function getAdminUserById(id: string) {
  return apiFetch<UserAdminDto>(`${BASE}/${id}`);
}

export function getRoles() {
  return apiFetch<RoleDto[]>(`${BASE}/roles`);
}

export function updateUserRole(userId: string, roleName: string) {
  return apiFetch<void>(`${BASE}/${userId}/role`, {
    method: "PATCH",
    body: JSON.stringify({ roleName }),
  });
}

export function activateUser(userId: string) {
  return apiFetch<void>(`${BASE}/${userId}/activate`, { method: "PATCH" });
}

export function deactivateUser(userId: string) {
  return apiFetch<void>(`${BASE}/${userId}/deactivate`, { method: "PATCH" });
}

export function inviteUser(data: InviteUserRequest) {
  return apiFetch<UserAdminDto>(`${BASE}/invite`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

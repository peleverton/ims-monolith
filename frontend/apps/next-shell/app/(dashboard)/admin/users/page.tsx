/**
 * Admin Users Page — US-040
 *
 * Server Component: busca dados iniciais de usuários e roles,
 * repassa ao UsersClient para CRUD interativo.
 * Requer autenticação com role Admin (guard no layout).
 */

import { apiFetch } from "@/lib/api-fetch";
import type { PagedResult, UserAdminDto, RoleDto } from "@/lib/types";
import { UsersClient } from "@/components/admin/users-client";

export const metadata = { title: "Gerenciar Usuários" };

interface SearchParams {
  page?: string;
  search?: string;
  role?: string;
}

async function fetchUsers(params: SearchParams) {
  const qs = new URLSearchParams({ page: params.page ?? "1", pageSize: "20" });
  if (params.search) qs.set("search", params.search);
  if (params.role) qs.set("role", params.role);
  return apiFetch<PagedResult<UserAdminDto>>(`/api/admin/users?${qs}`).catch(
    () => null
  );
}

async function fetchRoles() {
  return apiFetch<RoleDto[]>(`/api/admin/users/roles`).catch(() => []);
}

export default async function AdminUsersPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const params = await searchParams;
  const [data, roles] = await Promise.all([fetchUsers(params), fetchRoles()]);

  return (
    <div>
      <UsersClient
        initialData={data}
        initialRoles={roles}
        searchParams={params}
      />
    </div>
  );
}

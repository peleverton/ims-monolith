"use client";

/**
 * UsersClient — US-040
 *
 * Componente client-side para gerenciamento de usuários (admin).
 * Funcionalidades: listar, filtrar, convidar, alterar role, ativar/desativar.
 */

import { useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { UserPlus, ShieldCheck, UserX, UserCheck, Search } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { InviteUserDialog } from "@/components/admin/invite-user-dialog";
import { ChangeRoleDialog } from "@/components/admin/change-role-dialog";
import {
  getAdminUsers,
  getRoles,
  inviteUser,
  updateUserRole,
  activateUser,
  deactivateUser,
} from "@/lib/api/users";
import type {
  UserAdminDto,
  RoleDto,
  InviteUserRequest,
  PagedResult,
} from "@/lib/types";

interface Props {
  initialData: PagedResult<UserAdminDto> | null;
  initialRoles: RoleDto[];
  searchParams: { page?: string; search?: string; role?: string };
}

export function UsersClient({ initialData, initialRoles, searchParams }: Props) {
  const router = useRouter();
  const [data, setData] = useState(initialData);
  const [roles] = useState<RoleDto[]>(initialRoles);

  const [inviteOpen, setInviteOpen] = useState(false);
  const [roleOpen, setRoleOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<UserAdminDto | null>(null);

  const [search, setSearch] = useState(searchParams.search ?? "");
  const [roleFilter, setRoleFilter] = useState(searchParams.role ?? "");

  const refresh = useCallback(async (page = 1, s = search, r = roleFilter) => {
    try {
      const fresh = await getAdminUsers({ page, pageSize: 20, search: s || undefined, role: r || undefined });
      setData(fresh);
    } catch {
      toast.error("Erro ao atualizar lista de usuários");
    }
  }, [search, roleFilter]);

  // ── Invite ─────────────────────────────────────────────────────────────
  const handleInvite = async (values: InviteUserRequest) => {
    await inviteUser(values);
    toast.success(`Usuário ${values.username} convidado com sucesso!`);
    setInviteOpen(false);
    await refresh();
  };

  // ── Change Role ────────────────────────────────────────────────────────
  const openRoleChange = (user: UserAdminDto) => {
    setSelectedUser(user);
    setRoleOpen(true);
  };

  const handleRoleChange = async (roleName: string) => {
    if (!selectedUser) return;
    await updateUserRole(selectedUser.id, roleName);
    toast.success(`Role de ${selectedUser.username} atualizada para ${roleName}`);
    setRoleOpen(false);
    setSelectedUser(null);
    await refresh();
  };

  // ── Activate / Deactivate ───────────────────────────────────────────────
  const handleToggleActive = async (user: UserAdminDto) => {
    if (user.isActive) {
      await deactivateUser(user.id);
      toast.success(`${user.username} desativado`);
    } else {
      await activateUser(user.id);
      toast.success(`${user.username} ativado`);
    }
    await refresh();
  };

  // ── Filter ─────────────────────────────────────────────────────────────
  const applyFilter = async (e: React.FormEvent) => {
    e.preventDefault();
    const qs = new URLSearchParams();
    if (search) qs.set("search", search);
    if (roleFilter) qs.set("role", roleFilter);
    router.push(`/admin/users?${qs}`);
    await refresh(1, search, roleFilter);
  };

  return (
    <>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-(--text-primary)">Usuários</h1>
          <p className="text-(--text-secondary) text-sm mt-0.5">
            {data ? `${data.totalCount} usuários cadastrados` : "Carregando..."}
          </p>
        </div>
        <Button onClick={() => setInviteOpen(true)}>
          <UserPlus size={16} />
          Convidar usuário
        </Button>
      </div>

      {/* Filtros */}
      <form onSubmit={applyFilter} className="flex flex-wrap gap-3 mb-5">
        <div className="flex-1 min-w-48 relative">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-(--text-muted)" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar por nome, username, e-mail..."
            className="w-full pl-9 pr-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <select
          value={roleFilter}
          onChange={(e) => setRoleFilter(e.target.value)}
          className="px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Todas as roles</option>
          {roles.map((r) => (
            <option key={r.id} value={r.name}>{r.name}</option>
          ))}
        </select>
        <Button type="submit" variant="secondary">Filtrar</Button>
      </form>

      {/* Tabela */}
      {!data ? (
        <Card>
          <p className="text-center text-(--text-secondary) py-4">
            Erro ao carregar usuários.
          </p>
        </Card>
      ) : data.items.length === 0 ? (
        <Card>
          <p className="text-center text-(--text-secondary) py-8">
            Nenhum usuário encontrado.
          </p>
        </Card>
      ) : (
        <div className="bg-(--bg-surface) border border-(--border) rounded-xl overflow-hidden shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-(--bg-subtle) border-b border-(--border)">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Usuário
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Roles
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Status
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Último acesso
                </th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-(--border)">
              {data.items.map((user) => (
                <tr key={user.id} className="hover:bg-(--bg-subtle) transition-colors">
                  <td className="px-4 py-3">
                    <div className="font-medium text-(--text-primary)">{user.fullName}</div>
                    <div className="text-xs text-(--text-muted)">
                      @{user.username} · {user.email}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {user.roles.map((role) => (
                        <Badge
                          key={role}
                          variant={role === "Admin" ? "purple" : "info"}
                        >
                          {role}
                        </Badge>
                      ))}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={user.isActive ? "success" : "default"}>
                      {user.isActive ? "Ativo" : "Inativo"}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-(--text-secondary)">
                    {user.lastLoginAt
                      ? new Date(user.lastLoginAt).toLocaleString("pt-BR")
                      : "—"}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center justify-end gap-1">
                      <button
                        onClick={() => openRoleChange(user)}
                        title="Alterar role"
                        className="p-1.5 rounded-lg text-(--text-muted) hover:text-purple-600 hover:bg-purple-50 dark:hover:bg-purple-900/30 transition-colors"
                      >
                        <ShieldCheck size={15} />
                      </button>
                      <button
                        onClick={() => handleToggleActive(user)}
                        title={user.isActive ? "Desativar" : "Ativar"}
                        className={`p-1.5 rounded-lg transition-colors ${
                          user.isActive
                            ? "text-(--text-muted) hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/30"
                            : "text-(--text-muted) hover:text-green-600 hover:bg-green-50 dark:hover:bg-green-900/30"
                        }`}
                      >
                        {user.isActive ? <UserX size={15} /> : <UserCheck size={15} />}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Paginação */}
          <div className="px-4 py-3 border-t border-(--border) flex items-center justify-between text-sm text-(--text-secondary)">
            <span>
              Página {data.pageNumber} de {data.totalPages}
            </span>
            <div className="flex gap-2">
              {data.pageNumber > 1 && (
                <button
                  onClick={() => refresh(data.pageNumber - 1)}
                  className="px-3 py-1 rounded border border-(--border) hover:bg-(--bg-subtle)"
                >
                  Anterior
                </button>
              )}
              {data.pageNumber < data.totalPages && (
                <button
                  onClick={() => refresh(data.pageNumber + 1)}
                  className="px-3 py-1 rounded border border-(--border) hover:bg-(--bg-subtle)"
                >
                  Próxima
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Dialogs */}
      <InviteUserDialog
        open={inviteOpen}
        roles={roles}
        onClose={() => setInviteOpen(false)}
        onSubmit={handleInvite}
      />

      <ChangeRoleDialog
        open={roleOpen}
        user={selectedUser}
        roles={roles}
        onClose={() => { setRoleOpen(false); setSelectedUser(null); }}
        onSubmit={handleRoleChange}
      />
    </>
  );
}

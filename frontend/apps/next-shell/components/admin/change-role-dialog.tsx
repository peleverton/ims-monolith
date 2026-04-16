"use client";

/**
 * ChangeRoleDialog — US-040
 * Dialog para alterar a role de um usuário.
 */

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { Dialog } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import type { RoleDto, UserAdminDto } from "@/lib/types";

interface FormValues {
  roleName: string;
}

interface Props {
  open: boolean;
  user: UserAdminDto | null;
  roles: RoleDto[];
  onClose: () => void;
  onSubmit: (roleName: string) => Promise<void>;
}

export function ChangeRoleDialog({ open, user, roles, onClose, onSubmit }: Props) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { isSubmitting },
  } = useForm<FormValues>();

  useEffect(() => {
    if (user) reset({ roleName: user.roles[0] ?? "User" });
  }, [user, reset]);

  const handleFormSubmit = async (values: FormValues) => {
    await onSubmit(values.roleName);
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Alterar role"
      description={user ? `Usuário: ${user.username}` : undefined}
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" form="role-form" loading={isSubmitting}>
            Salvar
          </Button>
        </>
      }
    >
      <form id="role-form" onSubmit={handleSubmit(handleFormSubmit)}>
        <label className="block text-sm font-medium text-(--text-primary) mb-2">
          Nova role
        </label>
        <select
          {...register("roleName")}
          className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          {roles.map((r) => (
            <option key={r.id} value={r.name}>
              {r.name}
              {r.description ? ` — ${r.description}` : ""}
            </option>
          ))}
        </select>
      </form>
    </Dialog>
  );
}

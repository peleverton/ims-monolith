"use client";

/**
 * InviteUserDialog — US-040
 * Dialog para convidar novo usuário com username, email, nome e role.
 */

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Dialog } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import type { RoleDto } from "@/lib/types";

const schema = z.object({
  username: z
    .string()
    .min(3, "Mínimo 3 caracteres")
    .max(50)
    .regex(/^[a-zA-Z0-9_]+$/, "Apenas letras, números e _"),
  email: z.string().email("E-mail inválido").max(255),
  fullName: z.string().min(2, "Nome obrigatório").max(200),
  role: z.string().min(1, "Role obrigatória"),
});

type FormValues = z.infer<typeof schema>;

interface Props {
  open: boolean;
  roles: RoleDto[];
  onClose: () => void;
  onSubmit: (data: FormValues) => Promise<void>;
}

export function InviteUserDialog({ open, roles, onClose, onSubmit }: Props) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { role: "User" },
  });

  useEffect(() => {
    if (open) reset({ role: "User" });
  }, [open, reset]);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Convidar usuário"
      description="O usuário receberá uma senha temporária gerada automaticamente."
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button
            type="submit"
            form="invite-form"
            loading={isSubmitting}
          >
            Convidar
          </Button>
        </>
      }
    >
      <form
        id="invite-form"
        onSubmit={handleSubmit(onSubmit)}
        className="space-y-4"
      >
        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            Nome completo <span className="text-red-500">*</span>
          </label>
          <input
            {...register("fullName")}
            placeholder="João da Silva"
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.fullName && (
            <p className="mt-1 text-xs text-red-500">{errors.fullName.message}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            Username <span className="text-red-500">*</span>
          </label>
          <input
            {...register("username")}
            placeholder="joao.silva"
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.username && (
            <p className="mt-1 text-xs text-red-500">{errors.username.message}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            E-mail <span className="text-red-500">*</span>
          </label>
          <input
            {...register("email")}
            type="email"
            placeholder="joao@empresa.com"
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.email && (
            <p className="mt-1 text-xs text-red-500">{errors.email.message}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            Role <span className="text-red-500">*</span>
          </label>
          <select
            {...register("role")}
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {roles.map((r) => (
              <option key={r.id} value={r.name}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
      </form>
    </Dialog>
  );
}

"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import Link from "next/link";
import { UserPlus } from "lucide-react";

const schema = z.object({
  username: z.string().min(3, "Mínimo 3 caracteres"),
  email: z.string().email("E-mail inválido"),
  password: z.string().min(6, "Mínimo 6 caracteres"),
});
type FormData = z.infer<typeof schema>;

export default function RegisterPage() {
  const router = useRouter();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  const onSubmit = async (data: FormData) => {
    const res = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      toast.error(err.message ?? "Erro ao criar conta");
      return;
    }

    toast.success("Conta criada! Faça login para continuar.");
    router.push("/login");
  };

  return (
    <div className="bg-white/10 backdrop-blur-md rounded-2xl p-8 border border-white/20 shadow-2xl">
      <h2 className="text-xl font-semibold text-white mb-6">Criar nova conta</h2>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {[
          { name: "username" as const, label: "Usuário", placeholder: "seu.usuario", type: "text" },
          { name: "email" as const, label: "E-mail", placeholder: "email@exemplo.com", type: "email" },
          { name: "password" as const, label: "Senha", placeholder: "••••••••", type: "password" },
        ].map(({ name, label, placeholder, type }) => (
          <div key={name}>
            <label htmlFor={name} className="block text-sm font-medium text-blue-200 mb-1">{label}</label>
            <input
              {...register(name)}
              id={name}
              type={type}
              placeholder={placeholder}
              className="w-full px-4 py-2.5 rounded-lg bg-white/10 border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-blue-400"
            />
            {errors[name] && (
              <p className="text-red-400 text-xs mt-1">{errors[name]?.message}</p>
            )}
          </div>
        ))}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full flex items-center justify-center gap-2 py-2.5 px-4 bg-blue-600 hover:bg-blue-500 disabled:opacity-50 text-white font-medium rounded-lg transition-colors"
        >
          {isSubmitting ? (
            <span className="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full" />
          ) : (
            <UserPlus size={16} />
          )}
          {isSubmitting ? "Criando..." : "Criar conta"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-blue-300">
        Já tem conta?{" "}
        <Link href="/login" className="text-white font-medium hover:underline">
          Entrar
        </Link>
      </p>
    </div>
  );
}

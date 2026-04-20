"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import Link from "next/link";
import { ArrowLeft, Send } from "lucide-react";

const schema = z.object({
  title: z.string().min(5, "Mínimo 5 caracteres"),
  description: z.string().min(10, "Mínimo 10 caracteres"),
  priority: z.enum(["Low", "Medium", "High", "Critical"]),
});
type FormData = z.infer<typeof schema>;

export default function NewIssuePage() {
  const router = useRouter();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { priority: "Medium" },
  });

  const onSubmit = async (data: FormData) => {
    try {
      const res = await fetch("/api/proxy/issues", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(data),
      });

      if (!res.ok) {
        let errorMsg = `Erro ao criar issue (${res.status})`;
        try {
          const body = await res.json();
          console.error("[NewIssue] Error response:", res.status, body);
          if (body?.title) errorMsg = body.title;
          else if (body?.error) errorMsg = body.error;
          else if (body?.errors) errorMsg = Object.values(body.errors).flat().join(", ");
        } catch {}
        toast.error(errorMsg);
        return;
      }

      toast.success("Issue criada com sucesso!");
      // Force full navigation to ensure server components re-fetch fresh data
      window.location.href = "/issues";
    } catch (err) {
      console.error("[NewIssue] Fetch error:", err);
      toast.error("Erro de conexão com o servidor");
    }
  };

  return (
    <div className="max-w-2xl">
      <Link
        href="/issues"
        className="flex items-center gap-2 text-sm text-(--text-secondary) hover:text-(--text-primary) mb-6"
      >
        <ArrowLeft size={15} />
        Voltar para Issues
      </Link>

      <div className="bg-(--bg-surface) rounded-xl border border-(--border) shadow-sm p-6">
        <h1 className="text-xl font-bold text-(--text-primary) mb-6">Nova Issue</h1>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          <div>
            <label className="block text-sm font-medium text-(--text-primary) mb-1">Título</label>
            <input
              {...register("title")}
              className="w-full px-3 py-2.5 rounded-lg border border-(--border-input) bg-(--bg-surface) text-sm text-(--text-primary) focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Descreva o problema brevemente"
            />
            {errors.title && <p className="text-red-500 text-xs mt-1">{errors.title.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-(--text-primary) mb-1">Descrição</label>
            <textarea
              {...register("description")}
              rows={5}
              className="w-full px-3 py-2.5 rounded-lg border border-(--border-input) bg-(--bg-surface) text-sm text-(--text-primary) focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              placeholder="Descreva o problema em detalhes..."
            />
            {errors.description && <p className="text-red-500 text-xs mt-1">{errors.description.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-(--text-primary) mb-1">Prioridade</label>
            <select
              {...register("priority")}
              className="w-full px-3 py-2.5 rounded-lg border border-(--border-input) bg-(--bg-surface) text-sm text-(--text-primary) focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {["Low", "Medium", "High", "Critical"].map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>

          <div className="flex gap-3 pt-2">
            <Link
              href="/issues"
              className="flex-1 py-2.5 text-center rounded-lg border border-(--border-input) text-sm font-medium text-(--text-primary) hover:bg-(--bg-subtle) transition-colors"
            >
              Cancelar
            </Link>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex-1 flex items-center justify-center gap-2 py-2.5 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 disabled:opacity-50 transition-colors"
            >
              {isSubmitting ? (
                <span className="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full" />
              ) : <Send size={15} />}
              {isSubmitting ? "Criando..." : "Criar Issue"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

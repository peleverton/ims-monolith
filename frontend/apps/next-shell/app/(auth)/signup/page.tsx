"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import Link from "next/link";
import { Eye, EyeOff, Building2, CheckCircle2 } from "lucide-react";

const PLANS = [
  { id: "free", label: "Free", price: "$0/mo", description: "Up to 50 issues/month · 1 user" },
  { id: "starter", label: "Starter", price: "$29/mo", description: "Unlimited issues · 5 users" },
  { id: "pro", label: "Pro", price: "$99/mo", description: "Unlimited everything · priority support" },
] as const;

const schema = z.object({
  orgName: z.string().min(3, "Min 3 chars").max(50, "Max 50 chars"),
  email: z.string().email("Enter a valid email"),
  password: z.string().min(8, "Min 8 characters"),
  plan: z.enum(["free", "starter", "pro"]),
  hp_field: z.string().max(0, "Bot detected"),
});
type FormData = z.infer<typeof schema>;

export default function SignupPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [success, setSuccess] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { plan: "free", hp_field: "" },
  });

  const selectedPlan = watch("plan");

  const onSubmit = async (data: FormData) => {
    if (data.hp_field) {
      toast.error("Signup rejected.");
      return;
    }

    try {
      const res = await fetch("/api/auth/signup", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          orgName: data.orgName,
          email: data.email,
          password: data.password,
          plan: data.plan,
          hp_field: data.hp_field,
        }),
      });

      if (res.status === 409) {
        toast.error("An organisation with that name already exists. Try a different name.");
        return;
      }

      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        const detail =
          err?.errors
            ? Object.values(err.errors as Record<string, string[]>)
                .flat()
                .join(", ")
            : err?.message ?? "Signup failed. Please try again.";
        toast.error(detail);
        return;
      }

      const json = await res.json();
      setSuccess(json?.message ?? "Check your email to confirm your account.");
    } catch {
      toast.error("Unable to reach the server. Please try again.");
    }
  };

  if (success) {
    return (
      <div className="bg-white/10 backdrop-blur-md rounded-2xl p-8 border border-white/20 shadow-2xl text-center">
        <CheckCircle2 className="mx-auto text-green-400 mb-4" size={48} />
        <h2 className="text-xl font-semibold text-white mb-2">You&apos;re almost there!</h2>
        <p className="text-blue-200 text-sm">{success}</p>
        <Link
          href="/login"
          className="mt-6 inline-block text-sm text-blue-300 hover:text-white transition-colors"
        >
          ← Back to login
        </Link>
      </div>
    );
  }

  return (
    <div className="bg-white/10 backdrop-blur-md rounded-2xl p-8 border border-white/20 shadow-2xl">
      <h2 className="text-xl font-semibold text-white mb-6">Create your organisation</h2>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        {/* Honeypot — hidden from real users */}
        <input {...register("hp_field")} type="text" className="hidden" tabIndex={-1} autoComplete="off" />

        {/* Org Name */}
        <div>
          <label htmlFor="orgName" className="block text-sm font-medium text-blue-200 mb-1">
            Organisation name
          </label>
          <div className="relative">
            <input
              {...register("orgName")}
              id="orgName"
              type="text"
              placeholder="Acme Corp"
              className="w-full px-4 py-2.5 rounded-lg bg-white/10 border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-blue-400 pl-10"
            />
            <Building2
              size={16}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-white/40"
            />
          </div>
          {errors.orgName && (
            <p className="text-red-400 text-xs mt-1">{errors.orgName.message}</p>
          )}
        </div>

        {/* Email */}
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-blue-200 mb-1">
            Work email
          </label>
          <input
            {...register("email")}
            id="email"
            type="email"
            placeholder="you@company.com"
            className="w-full px-4 py-2.5 rounded-lg bg-white/10 border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-blue-400"
          />
          {errors.email && (
            <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>
          )}
        </div>

        {/* Password */}
        <div>
          <label htmlFor="password" className="block text-sm font-medium text-blue-200 mb-1">
            Password
          </label>
          <div className="relative">
            <input
              {...register("password")}
              id="password"
              type={showPassword ? "text" : "password"}
              placeholder="••••••••"
              className="w-full px-4 py-2.5 rounded-lg bg-white/10 border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-blue-400 pr-10"
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-white/50 hover:text-white"
            >
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          {errors.password && (
            <p className="text-red-400 text-xs mt-1">{errors.password.message}</p>
          )}
        </div>

        {/* Plan selector */}
        <div>
          <p className="block text-sm font-medium text-blue-200 mb-2">Plan</p>
          <div className="grid grid-cols-3 gap-2">
            {PLANS.map((plan) => (
              <button
                key={plan.id}
                type="button"
                onClick={() => setValue("plan", plan.id, { shouldValidate: true })}
                className={`rounded-lg p-3 text-left border transition-colors ${
                  selectedPlan === plan.id
                    ? "border-blue-400 bg-blue-500/20 text-white"
                    : "border-white/20 bg-white/5 text-white/70 hover:border-white/40"
                }`}
              >
                <div className="text-xs font-bold">{plan.label}</div>
                <div className="text-xs font-semibold text-blue-300 mt-0.5">{plan.price}</div>
                <div className="text-[10px] text-white/50 mt-1 leading-tight">{plan.description}</div>
              </button>
            ))}
          </div>
          {errors.plan && (
            <p className="text-red-400 text-xs mt-1">{errors.plan.message}</p>
          )}
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full flex items-center justify-center gap-2 py-2.5 px-4 bg-blue-600 hover:bg-blue-500 disabled:opacity-50 text-white font-medium rounded-lg transition-colors"
        >
          {isSubmitting ? (
            <span className="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full" />
          ) : null}
          {isSubmitting ? "Creating account…" : "Create account"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-blue-300">
        Already have an account?{" "}
        <Link href="/login" className="text-white font-medium hover:underline">
          Sign in
        </Link>
      </p>
    </div>
  );
}

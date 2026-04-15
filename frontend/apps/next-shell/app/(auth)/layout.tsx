export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-linear-to-br from-slate-900 via-blue-950 to-slate-900 px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-white tracking-tight">IMS</h1>
          <p className="text-blue-300 text-sm mt-1">Inventory Management System</p>
        </div>
        {children}
      </div>
    </div>
  );
}

import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { ShieldCheck, Eye, EyeOff, AlertCircle } from 'lucide-react';

export default function LoginPage() {
  const { login } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !password.trim()) {
      setError('Username and password are required.');
      return;
    }
    setLoading(true);
    setError(null);
    try {
      await login(username.trim(), password);
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data ||
        'Invalid username or password.';
      setError(typeof msg === 'string' ? msg : 'Login failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-[#004282] via-[#003370] to-[#001f4d] flex items-center justify-center p-4">
      {/* Background pattern */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-24 -right-24 w-96 h-96 bg-white/5 rounded-full" />
        <div className="absolute -bottom-32 -left-16 w-80 h-80 bg-white/5 rounded-full" />
        <div className="absolute top-1/3 left-1/4 w-64 h-64 bg-[#d32f2f]/10 rounded-full blur-3xl" />
      </div>

      <div className="relative w-full max-w-md">
        {/* Card */}
        <div className="bg-white rounded-2xl shadow-[0_25px_60px_rgba(0,0,0,0.35)] overflow-hidden">
          {/* Header stripe */}
          <div className="h-1.5" style={{ background: 'linear-gradient(to right, #d32f2f 30%, #004282 100%)' }} />

          {/* Brand section */}
          <div className="px-8 pt-8 pb-6 text-center border-b border-slate-100">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-[#004282] shadow-lg mb-4">
              <ShieldCheck size={28} className="text-white" />
            </div>
            <h1 className="text-2xl font-extrabold text-[#004282]">PrecisionPro</h1>
            <p className="text-slate-500 text-sm mt-1">Precision-driven Insurance Calculations</p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="px-8 py-7 space-y-5">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                Username
              </label>
              <input
                type="text"
                autoComplete="username"
                value={username}
                onChange={e => setUsername(e.target.value)}
                placeholder="Enter your username"
                className="w-full px-4 py-2.5 rounded-xl border border-slate-200 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#004282] focus:border-[#004282]
                           placeholder:text-slate-400 transition"
              />
            </div>

            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                Password
              </label>
              <div className="relative">
                <input
                  type={showPw ? 'text' : 'password'}
                  autoComplete="current-password"
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder="Enter your password"
                  className="w-full px-4 py-2.5 pr-11 rounded-xl border border-slate-200 text-sm
                             focus:outline-none focus:ring-2 focus:ring-[#004282] focus:border-[#004282]
                             placeholder:text-slate-400 transition"
                />
                <button
                  type="button"
                  onClick={() => setShowPw(v => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                >
                  {showPw ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>

            {error && (
              <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700">
                <AlertCircle size={15} className="flex-shrink-0 mt-0.5" />
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                         hover:bg-[#003370] disabled:opacity-60 transition-colors
                         flex items-center justify-center gap-2 shadow-md"
            >
              {loading ? (
                <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <ShieldCheck size={16} />
              )}
              {loading ? 'Signing in…' : 'Login'}
            </button>

            <div className="text-center">
              <button
                type="button"
                className="text-xs text-[#004282] underline hover:text-[#003370]"
                onClick={() => setError('Please contact your system administrator to reset your password.')}
              >
                Forgot Password?
              </button>
            </div>
          </form>

          {/* Demo hint */}
          <div className="px-8 pb-7">
            <div className="bg-blue-50 border border-blue-100 rounded-xl p-3 text-xs text-slate-500 text-center">
              Demo credentials: <span className="font-semibold text-[#004282]">admin</span> / <span className="font-semibold text-[#004282]">admin123</span>
            </div>
          </div>
        </div>

        <p className="text-center text-blue-200/60 text-xs mt-6">
          © {new Date().getFullYear()} PrecisionPro · All calculations are illustrative only
        </p>
      </div>
    </div>
  );
}

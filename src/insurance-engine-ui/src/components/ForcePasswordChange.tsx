import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { ShieldCheck, Eye, EyeOff, AlertCircle, Lock } from 'lucide-react';

export default function ForcePasswordChange() {
  const { changePassword, logout } = useAuth();
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!newPassword || !confirmPassword) {
      setError('Both fields are required.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }

    if (!/[A-Z]/.test(newPassword)) {
      setError('Password must contain at least one uppercase letter.');
      return;
    }

    if (!/\d/.test(newPassword)) {
      setError('Password must contain at least one number.');
      return;
    }

    if (!/[^a-zA-Z0-9]/.test(newPassword)) {
      setError('Password must contain at least one special character.');
      return;
    }

    setLoading(true);
    try {
      await changePassword(newPassword);
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.response?.data ||
        'Failed to change password. Please try again.';
      setError(typeof msg === 'string' ? msg : 'Password change failed.');
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
        <div className="bg-white rounded-2xl shadow-[0_25px_60px_rgba(0,0,0,0.35)] overflow-hidden">
          {/* Header stripe */}
          <div className="h-1.5" style={{ background: 'linear-gradient(to right, #d32f2f 30%, #004282 100%)' }} />

          {/* Brand section */}
          <div className="px-8 pt-8 pb-6 text-center border-b border-slate-100">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-amber-500 shadow-lg mb-4">
              <Lock size={28} className="text-white" />
            </div>
            <h1 className="text-2xl font-extrabold text-[#004282]">Change Password</h1>
            <p className="text-slate-500 text-sm mt-1">
              You must set a new password before accessing the system.
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="px-8 py-7 space-y-5">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                New Password
              </label>
              <div className="relative">
                <input
                  type={showNew ? 'text' : 'password'}
                  autoComplete="new-password"
                  value={newPassword}
                  onChange={e => setNewPassword(e.target.value)}
                  placeholder="Enter new password"
                  className="w-full px-4 py-2.5 pr-11 rounded-xl border border-slate-200 text-sm
                             focus:outline-none focus:ring-2 focus:ring-[#004282] focus:border-[#004282]
                             placeholder:text-slate-400 transition"
                />
                <button
                  type="button"
                  onClick={() => setShowNew(v => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                >
                  {showNew ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>

            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1.5">
                Confirm New Password
              </label>
              <div className="relative">
                <input
                  type={showConfirm ? 'text' : 'password'}
                  autoComplete="new-password"
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
                  placeholder="Re-enter new password"
                  className="w-full px-4 py-2.5 pr-11 rounded-xl border border-slate-200 text-sm
                             focus:outline-none focus:ring-2 focus:ring-[#004282] focus:border-[#004282]
                             placeholder:text-slate-400 transition"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirm(v => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                >
                  {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>

            {/* Password requirements */}
            <div className="bg-blue-50 border border-blue-100 rounded-xl p-3 text-xs text-slate-600">
              <p className="font-semibold text-slate-700 mb-1">Password requirements:</p>
              <ul className="list-disc list-inside space-y-0.5">
                <li>At least 8 characters</li>
                <li>At least one uppercase letter</li>
                <li>At least one number</li>
                <li>At least one special character</li>
              </ul>
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
              {loading ? 'Changing…' : 'Set New Password'}
            </button>

            <div className="text-center">
              <button
                type="button"
                className="text-xs text-slate-400 hover:text-slate-600"
                onClick={logout}
              >
                Cancel &amp; Return to Login
              </button>
            </div>
          </form>
        </div>

        <p className="text-center text-blue-200/60 text-xs mt-6">
          © {new Date().getFullYear()} PrecisionPro · All calculations are illustrative only
        </p>
      </div>
    </div>
  );
}

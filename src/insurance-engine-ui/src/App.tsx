import { useState, useRef, useEffect } from 'react';
import {
  LayoutDashboard,
  TrendingUp,
  BarChart3,
  ClipboardCheck,
  Settings,
  LogOut,
  ShieldCheck,
  ChevronDown,
} from 'lucide-react';
import { AuthProvider, useAuth } from './context/AuthContext';
import LoginPage from './components/LoginPage';
import Dashboard from './components/Dashboard';
import BenefitIllustration from './components/BenefitIllustration';
import UlipIllustration from './components/UlipIllustration';
import YpygModule from './components/YpygModule';
import AuditModule from './components/AuditModule';
import AdminMaster from './components/AdminMaster';

// ---------------------------------------------------------------------------
// View IDs
// ---------------------------------------------------------------------------
type ViewId =
  | 'dashboard'
  | 'bi-endowment'
  | 'bi-ulip'
  | 'ypyg-policy'
  | 'ypyg-input'
  | 'audit-payout'
  | 'audit-bonus'
  | 'admin-master';

// ---------------------------------------------------------------------------
// Dropdown nav item
// ---------------------------------------------------------------------------
interface DropdownItem {
  id: ViewId;
  label: string;
}

interface NavItem {
  id: ViewId | string;
  label: string;
  icon: React.ReactNode;
  children?: DropdownItem[];
}

const NAV_ITEMS: NavItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: <LayoutDashboard size={15} /> },
  {
    id: 'bi',
    label: 'Benefit Illustration',
    icon: <TrendingUp size={15} />,
    children: [
      { id: 'bi-endowment', label: 'Endowment' },
      { id: 'bi-ulip',      label: 'ULIP' },
    ],
  },
  {
    id: 'ypyg',
    label: 'YPYG',
    icon: <BarChart3 size={15} />,
    children: [
      { id: 'ypyg-policy', label: 'Policy Number' },
      { id: 'ypyg-input',  label: 'Input Value' },
    ],
  },
  {
    id: 'audit',
    label: 'Audit',
    icon: <ClipboardCheck size={15} />,
    children: [
      { id: 'audit-payout', label: 'Payout Verification' },
      { id: 'audit-bonus',  label: 'Addition / Bonus' },
    ],
  },
  { id: 'admin-master', label: 'Admin Master', icon: <Settings size={15} /> },
];

// ---------------------------------------------------------------------------
// Dropdown component
// ---------------------------------------------------------------------------
function NavDropdown({
  item,
  activeView,
  onSelect,
}: {
  item: NavItem;
  activeView: ViewId;
  onSelect: (id: ViewId) => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const isActive = item.children?.some(c => c.id === activeView) ?? false;

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(v => !v)}
        className={`
          flex items-center gap-2 px-4 py-2 rounded-full text-sm font-semibold
          whitespace-nowrap transition-all duration-200 select-none
          ${isActive
            ? 'bg-[#004282] text-white shadow-md'
            : 'bg-white text-[#004282] border border-[#004282] hover:bg-blue-50'}
        `}
      >
        {item.icon}
        {item.label}
        <ChevronDown size={12} className={`transition-transform ${open ? 'rotate-180' : ''}`} />
      </button>

      {open && (
        <div className="absolute top-full left-0 mt-1 bg-white border border-slate-200 rounded-xl shadow-xl z-50 min-w-[170px] overflow-hidden">
          {item.children!.map(child => (
            <button
              key={child.id}
              onClick={() => { onSelect(child.id); setOpen(false); }}
              className={`
                w-full text-left px-4 py-2.5 text-sm transition-colors
                ${activeView === child.id
                  ? 'bg-[#004282] text-white font-semibold'
                  : 'hover:bg-blue-50 text-slate-700'}
              `}
            >
              {child.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main app (requires auth)
// ---------------------------------------------------------------------------
function AppInner() {
  const { user, logout } = useAuth();
  const [activeView, setActiveView] = useState<ViewId>('dashboard');

  return (
    <div className="min-h-screen bg-slate-50">
      {/* ── Header ── */}
      <header className="bg-[#004282] text-white">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 bg-white rounded-xl flex items-center justify-center flex-shrink-0 shadow">
              <ShieldCheck size={20} className="text-[#004282]" />
            </div>
            <div>
              <h1 className="text-lg font-extrabold leading-tight tracking-tight">PrecisionPro</h1>
              <p className="text-blue-200 text-xs">Precision-driven Insurance Calculations</p>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <span className="hidden sm:inline-flex items-center px-3 py-1 rounded-full bg-white/10
                             text-blue-100 text-xs font-medium border border-white/20">
              {user?.username} · {user?.role}
            </span>
            <button
              onClick={logout}
              title="Logout"
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white/10
                         hover:bg-white/20 text-white text-xs font-semibold transition"
            >
              <LogOut size={14} />
              Logout
            </button>
          </div>
        </div>
        <div className="h-1" style={{ background: 'linear-gradient(to right, #d32f2f 40%, #004282 100%)' }} />
      </header>

      {/* ── Navigation ── */}
      <nav className="bg-white border-b border-slate-200 shadow-sm sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex gap-2 py-3 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
            {NAV_ITEMS.map(item => {
              if (item.children) {
                return (
                  <NavDropdown
                    key={item.id}
                    item={item}
                    activeView={activeView}
                    onSelect={setActiveView}
                  />
                );
              }
              const viewId = item.id as ViewId;
              return (
                <button
                  key={item.id}
                  onClick={() => setActiveView(viewId)}
                  className={`
                    flex items-center gap-2 px-4 py-2 rounded-full text-sm font-semibold
                    whitespace-nowrap transition-all duration-200 select-none
                    ${activeView === viewId
                      ? 'bg-[#004282] text-white shadow-md'
                      : 'bg-white text-[#004282] border border-[#004282] hover:bg-blue-50'}
                  `}
                >
                  {item.icon}
                  {item.label}
                </button>
              );
            })}
          </div>
        </div>
      </nav>

      {/* ── Page content ── */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        {activeView === 'dashboard'     && <Dashboard />}
        {activeView === 'bi-endowment'  && <BenefitIllustration />}
        {activeView === 'bi-ulip'       && <UlipIllustration />}
        {activeView === 'ypyg-policy'   && <YpygModule mode="policy-number" />}
        {activeView === 'ypyg-input'    && <YpygModule mode="input-value" />}
        {activeView === 'audit-payout'  && <AuditModule sub="payout-verification" />}
        {activeView === 'audit-bonus'   && <AuditModule sub="addition-bonus" />}
        {activeView === 'admin-master'  && <AdminMaster />}
      </main>

      <footer className="border-t border-slate-200 mt-12 py-4 text-center text-xs text-slate-400">
        © {new Date().getFullYear()} PrecisionPro · All calculations are illustrative only
      </footer>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Root — login gate
// ---------------------------------------------------------------------------
function AppWithAuth() {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <AppInner /> : <LoginPage />;
}

export default function App() {
  return (
    <AuthProvider>
      <AppWithAuth />
    </AuthProvider>
  );
}

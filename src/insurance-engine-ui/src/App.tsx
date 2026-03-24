import { useState, useRef, useEffect, useCallback } from 'react';
import {
  LayoutDashboard,
  TrendingUp,
  BarChart3,
  ClipboardCheck,
  Settings,
  Users,
  LogOut,
  ShieldCheck,
  ChevronDown,
  ShieldOff,
  Bell,
} from 'lucide-react';
import { AuthProvider, useAuth } from './context/AuthContext';
import { PermissionProvider, usePermission } from './context/PermissionContext';
import { api } from './api';
import LoginPage from './components/LoginPage';
import ForcePasswordChange from './components/ForcePasswordChange';
import Dashboard from './components/Dashboard';
import BenefitIllustration from './components/BenefitIllustration';
import UlipIllustration from './components/UlipIllustration';
import YpygModule from './components/YpygModule';
import AuditModule from './components/AuditModule';
import PayoutVerification from './components/PayoutVerification';
import Configuration from './components/Configuration';
import UserManagement from './components/UserManagement';

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
  | 'payout-verify'
  | 'configuration'
  | 'user-mgmt';

// ---------------------------------------------------------------------------
// Map view IDs to permission module codes
// ---------------------------------------------------------------------------
const VIEW_MODULE_MAP: Record<string, string> = {
  'bi': 'BI',
  'bi-endowment': 'BI',
  'bi-ulip': 'BI',
  'ypyg': 'YPYG',
  'ypyg-policy': 'YPYG',
  'ypyg-input': 'YPYG',
  'audit': 'AUDIT',
  'audit-payout': 'AUDIT',
  'audit-bonus': 'AUDIT',
  'payout-verify': 'AUDIT',
  'configuration': 'CONFIG',
  'user-mgmt': 'USERMGMT',
};

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
      { id: 'payout-verify', label: 'Payout Verification' },
      { id: 'audit-payout', label: 'Audit — Payout' },
      { id: 'audit-bonus',  label: 'Audit — Bonus' },
    ],
  },
  { id: 'configuration', label: 'Configuration', icon: <Settings size={15} /> },
  { id: 'user-mgmt', label: 'User Mgmt', icon: <Users size={15} /> },
];

// ---------------------------------------------------------------------------
// Notification bell component
// ---------------------------------------------------------------------------
interface NotifItem { id: number; message: string; relatedModule?: string; relatedId?: string; createdAt: string }

function NotificationBell({ onNavigate }: { onNavigate?: (view: ViewId) => void }) {
  const [items, setItems] = useState<NotifItem[]>([]);
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const fetchNotifications = useCallback(async () => {
    try {
      const res = await api.get<NotifItem[]>('/notifications');
      setItems(res.data ?? []);
    } catch { /* ignore fetch errors */ }
  }, []);

  useEffect(() => {
    fetchNotifications();
    const interval = setInterval(fetchNotifications, 60_000);
    return () => clearInterval(interval);
  }, [fetchNotifications]);

  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  const markRead = async (id: number) => {
    try { await api.put(`/notifications/${id}/read`); } catch { /* ignore */ }
    setItems(prev => prev.filter(n => n.id !== id));
  };

  const markAllRead = async () => {
    try { await api.put('/notifications/read-all'); } catch { /* ignore */ }
    setItems([]);
  };

  return (
    <div ref={ref} className="relative">
      <button onClick={() => setOpen(!open)} title="Notifications"
        className="relative flex items-center justify-center w-8 h-8 rounded-lg bg-white/10 hover:bg-white/20 transition">
        <Bell size={16} />
        {items.length > 0 && (
          <span className="absolute -top-1 -right-1 min-w-[18px] h-[18px] flex items-center justify-center
                           bg-red-500 text-white text-[10px] font-bold rounded-full px-1">
            {items.length > 99 ? '99+' : items.length}
          </span>
        )}
      </button>
      {open && (
        <div className="absolute right-0 top-10 w-80 bg-white rounded-xl shadow-xl border border-slate-200 z-50 max-h-96 overflow-y-auto">
          <div className="flex items-center justify-between px-4 py-2 border-b border-slate-100">
            <span className="text-sm font-semibold text-slate-700">Notifications</span>
            {items.length > 0 && (
              <button onClick={markAllRead} className="text-xs text-blue-600 hover:underline">Mark all read</button>
            )}
          </div>
          {items.length === 0 ? (
            <div className="px-4 py-6 text-center text-sm text-slate-400">No unread notifications</div>
          ) : (
            items.map(n => (
              <div key={n.id}
                className="px-4 py-3 border-b border-slate-50 hover:bg-blue-50 cursor-pointer text-sm text-slate-700"
                onClick={() => {
                  markRead(n.id);
                  if (n.relatedModule === 'PayoutVerification' && onNavigate) onNavigate('payout-verify');
                  setOpen(false);
                }}>
                <p className="text-slate-800 text-xs leading-relaxed">{n.message}</p>
                <p className="text-slate-400 text-[10px] mt-1">
                  {new Date(n.createdAt).toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                </p>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}

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
  const [dropdownPos, setDropdownPos] = useState({ top: 0, left: 0 });
  const buttonRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const isActive = item.children?.some(c => c.id === activeView) ?? false;

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      const target = e.target as Node;
      const isClickedInsideButton = buttonRef.current?.contains(target);
      const isClickedInsideDropdown = dropdownRef.current?.contains(target);
      
      if (!isClickedInsideButton && !isClickedInsideDropdown) {
        setOpen(false);
      }
    };
    if (open) {
      document.addEventListener('mousedown', handler);
      return () => document.removeEventListener('mousedown', handler);
    }
  }, [open]);

  const handleClick = () => {
    if (!open && buttonRef.current) {
      const rect = buttonRef.current.getBoundingClientRect();
      setDropdownPos({
        top: rect.bottom + 8,
        left: rect.left,
      });
    }
    setOpen(v => !v);
  };

  const handleMenuClick = (childId: ViewId) => {
    onSelect(childId);
    setOpen(false);
  };

  return (
    <div>
      <button
        ref={buttonRef}
        onClick={handleClick}
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
        <div 
          ref={dropdownRef}
          className="fixed bg-white border border-slate-200 rounded-xl shadow-2xl min-w-[170px] overflow-hidden"
          style={{
            top: `${dropdownPos.top}px`,
            left: `${dropdownPos.left}px`,
            zIndex: 9999,
          }}
        >
          {item.children!.map(child => (
            <button
              key={child.id}
              onClick={() => handleMenuClick(child.id)}
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
// Access Denied component
// ---------------------------------------------------------------------------
function AccessDenied() {
  return (
    <div className="flex flex-col items-center justify-center py-20 text-center">
      <ShieldOff size={48} className="text-red-400 mb-4" />
      <h2 className="text-xl font-bold text-slate-700 mb-2">Access Denied</h2>
      <p className="text-slate-500 text-sm max-w-md">
        You do not have permission to access this module. Please contact your administrator
        to request access.
      </p>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main app (requires auth)
// ---------------------------------------------------------------------------
function AppInner() {
  const { user, logout } = useAuth();
  const { hasAccess } = usePermission();
  const [activeView, setActiveView] = useState<ViewId>('dashboard');

  // Filter nav items based on permissions
  const visibleNavItems = NAV_ITEMS.filter(item => {
    const moduleCode = VIEW_MODULE_MAP[item.id];
    // Dashboard is always visible; for items with module codes, check permission
    if (!moduleCode) return true;
    return hasAccess(moduleCode);
  });

  // Check if active view is allowed — show access denied if not
  const activeModuleCode = VIEW_MODULE_MAP[activeView];
  const isAllowed = !activeModuleCode || hasAccess(activeModuleCode);

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
            <NotificationBell onNavigate={setActiveView} />
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
      <nav className="bg-white border-b border-slate-200 shadow-sm sticky top-0 z-20">
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex gap-2 py-3 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
            {visibleNavItems.map(item => {
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
        {!isAllowed && <AccessDenied />}
        {isAllowed && activeView === 'dashboard'     && <Dashboard />}
        {isAllowed && activeView === 'bi-endowment'  && <BenefitIllustration />}
        {isAllowed && activeView === 'bi-ulip'       && <UlipIllustration />}
        {isAllowed && activeView === 'ypyg-policy'   && <YpygModule mode="policy-number" />}
        {isAllowed && activeView === 'ypyg-input'    && <YpygModule mode="input-value" />}
        {isAllowed && activeView === 'audit-payout'    && <AuditModule sub="payout-verification" subOption="single" />}
        {isAllowed && activeView === 'audit-bonus'     && <AuditModule sub="addition-bonus" subOption="single" />}
        {isAllowed && activeView === 'payout-verify'   && <PayoutVerification />}
        {isAllowed && activeView === 'configuration'   && <Configuration />}
        {isAllowed && activeView === 'user-mgmt'       && <UserManagement />}
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
  const { isAuthenticated, forcePasswordChange } = useAuth();
  if (forcePasswordChange) return <ForcePasswordChange />;
  return isAuthenticated ? <AppInner /> : <LoginPage />;
}

export default function App() {
  return (
    <AuthProvider>
      <PermissionProvider>
        <AppWithAuth />
      </PermissionProvider>
    </AuthProvider>
  );
}

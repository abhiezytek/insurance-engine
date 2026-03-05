import { useState, useEffect } from 'react';
import {
  BarChart3,
  Upload as UploadIcon,
  Package,
  TrendingUp,
  ClipboardList,
  Settings,
  LineChart,
} from 'lucide-react';
import Calculator from './components/Calculator';
import Products from './components/Products';
import Upload from './components/Upload';
import BenefitIllustration from './components/BenefitIllustration';
import AuditLog from './components/AuditLog';
import UlipIllustration from './components/UlipIllustration';
import ModuleSettings from './components/ModuleSettings';
import { ModuleProvider, useModules, type ModuleId } from './context/ModuleContext';

// ---------------------------------------------------------------------------
// Icon map — one icon per module ID
// ---------------------------------------------------------------------------
const MODULE_ICONS: Record<ModuleId, React.ReactNode> = {
  bi:       <TrendingUp size={16} />,
  ypyg:     <BarChart3 size={16} />,
  ulip:     <LineChart size={16} />,
  products: <Package size={16} />,
  upload:   <UploadIcon size={16} />,
  audit:    <ClipboardList size={16} />,
};

// ---------------------------------------------------------------------------
// Inner app — consumes ModuleContext
// ---------------------------------------------------------------------------
function AppInner() {
  const { enabledModules } = useModules();
  const [activeTab, setActiveTab] = useState<ModuleId | null>(null);
  const [settingsOpen, setSettingsOpen] = useState(false);

  // Keep activeTab pointing at a visible, enabled tab
  useEffect(() => {
    const ids = enabledModules.map(m => m.id);
    if (ids.length === 0) {
      setActiveTab(null);
      return;
    }
    // If current tab is still enabled, keep it
    if (activeTab && ids.includes(activeTab)) return;
    // Otherwise default to first enabled module
    setActiveTab(ids[0]);
  }, [enabledModules, activeTab]);

  return (
    <div className="min-h-screen bg-slate-50">
      {/* ------------------------------------------------------------------ */}
      {/* Top header                                                          */}
      {/* ------------------------------------------------------------------ */}
      <header className="bg-[#004282] text-white">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-white rounded-lg flex items-center justify-center flex-shrink-0">
              <span className="text-[#004282] font-extrabold text-sm">SI</span>
            </div>
            <div>
              <h1 className="text-lg font-bold leading-tight">SUD Life Insurance Engine</h1>
              <p className="text-blue-200 text-xs">Century Income — Benefit Illustration System</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <span className="hidden sm:inline-flex items-center px-3 py-1 rounded-full bg-white/10
                             text-blue-100 text-xs font-medium border border-white/20">
              Non-Linked · Non-Participating · Savings
            </span>

            {/* Module settings gear */}
            <button
              onClick={() => setSettingsOpen(true)}
              title="Module Settings"
              aria-label="Open module settings"
              className="p-2 rounded-lg hover:bg-white/10 transition-colors text-white"
            >
              <Settings size={18} />
            </button>
          </div>
        </div>
        {/* Red→Navy gradient accent bar */}
        <div className="h-1" style={{ background: 'linear-gradient(to right, #d32f2f 40%, #004282 100%)' }} />
      </header>

      {/* ------------------------------------------------------------------ */}
      {/* Navigation — only enabled modules                                   */}
      {/* ------------------------------------------------------------------ */}
      <nav className="bg-white border-b border-slate-200 shadow-sm sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-6">
          {enabledModules.length > 0 ? (
            <div className="flex gap-2 py-3 overflow-x-auto [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
              {enabledModules.map(mod => (
                <button
                  key={mod.id}
                  onClick={() => setActiveTab(mod.id)}
                  className={`
                    flex items-center gap-2 px-4 py-2 rounded-full text-sm font-semibold
                    whitespace-nowrap transition-all duration-200 select-none
                    ${activeTab === mod.id
                      ? 'bg-[#004282] text-white shadow-md'
                      : 'bg-white text-[#004282] border border-[#004282] hover:bg-blue-50'
                    }
                  `}
                >
                  {MODULE_ICONS[mod.id]}
                  {mod.label}
                </button>
              ))}
            </div>
          ) : (
            <div className="py-4 text-center text-sm text-slate-400">
              All modules are disabled.{' '}
              <button
                onClick={() => setSettingsOpen(true)}
                className="text-[#007bff] underline"
              >
                Open Module Settings
              </button>{' '}
              to enable one.
            </div>
          )}
        </div>
      </nav>

      {/* ------------------------------------------------------------------ */}
      {/* Page content                                                        */}
      {/* ------------------------------------------------------------------ */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        {activeTab === 'bi'       && <BenefitIllustration />}
        {activeTab === 'ypyg'     && <Calculator />}
        {activeTab === 'ulip'     && <UlipIllustration />}
        {activeTab === 'products' && <Products />}
        {activeTab === 'upload'   && <Upload />}
        {activeTab === 'audit'    && <AuditLog />}
        {activeTab === null && enabledModules.length === 0 && (
          <div className="flex flex-col items-center justify-center py-32 text-slate-400 space-y-4">
            <Settings size={48} className="text-slate-200" />
            <p className="text-lg font-semibold">No modules enabled</p>
            <p className="text-sm">Use the ⚙ settings button in the header to enable modules.</p>
          </div>
        )}
      </main>

      <footer className="border-t border-slate-200 mt-12 py-4 text-center text-xs text-slate-400">
        © {new Date().getFullYear()} SUD Life Insurance Engine · All calculations are illustrative only
      </footer>

      {/* Module settings drawer */}
      <ModuleSettings open={settingsOpen} onClose={() => setSettingsOpen(false)} />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Root export — wraps with ModuleProvider
// ---------------------------------------------------------------------------
export default function App() {
  return (
    <ModuleProvider>
      <AppInner />
    </ModuleProvider>
  );
}

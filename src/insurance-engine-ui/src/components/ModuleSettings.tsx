import { X, Lock, ToggleLeft, ToggleRight, Info } from 'lucide-react';
import { useModules, type ModuleId } from '../context/ModuleContext';

interface ModuleSettingsProps {
  open: boolean;
  onClose: () => void;
}

export default function ModuleSettings({ open, onClose }: ModuleSettingsProps) {
  const { modules, toggleModule } = useModules();

  if (!open) return null;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/30 backdrop-blur-sm z-40"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Drawer */}
      <aside
        role="dialog"
        aria-label="Module Settings"
        className="fixed top-0 right-0 h-full w-80 bg-white shadow-2xl z-50 flex flex-col"
      >
        {/* Header */}
        <div className="bg-[#004282] text-white px-6 py-5 flex items-center justify-between flex-shrink-0">
          <div>
            <h2 className="font-bold text-base">Module Settings</h2>
            <p className="text-blue-200 text-xs mt-0.5">Enable or disable application modules</p>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg hover:bg-white/10 transition-colors"
            aria-label="Close settings"
          >
            <X size={18} />
          </button>
        </div>

        {/* Module list */}
        <div className="flex-1 overflow-y-auto py-4 px-4 space-y-3">
          {modules.map(mod => (
            <ModuleRow
              key={mod.id}
              id={mod.id}
              label={mod.label}
              description={mod.description}
              enabled={mod.enabled}
              userToggleable={mod.userToggleable}
              onToggle={enabled => toggleModule(mod.id, enabled)}
            />
          ))}
        </div>

        {/* Footer */}
        <div className="border-t border-slate-100 px-4 py-4 flex-shrink-0 space-y-2">
          <div className="flex items-start gap-2 text-xs text-slate-400">
            <Info size={12} className="mt-0.5 flex-shrink-0" />
            <span>
              Changes are saved to your browser and take effect immediately. Modules marked
              with a lock icon are controlled by environment variables and cannot be toggled
              at runtime.
            </span>
          </div>
          <div className="flex items-start gap-2 text-xs text-slate-400">
            <span className="font-mono bg-slate-50 px-1 rounded text-[10px]">VITE_ENABLED_MODULES</span>
            <span>— set comma-separated module IDs at deploy time to restrict access.</span>
          </div>
        </div>
      </aside>
    </>
  );
}

// ---------------------------------------------------------------------------
// Row
// ---------------------------------------------------------------------------

interface ModuleRowProps {
  id: ModuleId;
  label: string;
  description: string;
  enabled: boolean;
  userToggleable: boolean;
  onToggle: (enabled: boolean) => void;
}

function ModuleRow({ id, label, description, enabled, userToggleable, onToggle }: ModuleRowProps) {
  const colorMap: Record<ModuleId, string> = {
    bi:       'bg-indigo-100 text-indigo-600',
    ypyg:     'bg-emerald-100 text-emerald-600',
    products: 'bg-amber-100 text-amber-600',
    upload:   'bg-blue-100 text-blue-600',
    audit:    'bg-rose-100 text-rose-600',
  };

  const abbreviationMap: Record<ModuleId, string> = {
    bi:       'BI',
    ypyg:     'YG',
    products: 'PF',
    upload:   'UP',
    audit:    'AL',
  };

  return (
    <div
      className={`
        rounded-xl border p-4 transition-all
        ${enabled ? 'border-slate-200 bg-white' : 'border-slate-100 bg-slate-50'}
      `}
    >
      <div className="flex items-start gap-3">
        {/* Avatar */}
        <div
          className={`w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0 text-xs font-bold
            ${enabled ? colorMap[id] : 'bg-slate-100 text-slate-400'}`}
        >
          {abbreviationMap[id]}
        </div>

        {/* Text */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className={`text-sm font-semibold ${enabled ? 'text-[#004282]' : 'text-slate-400'}`}>
              {label}
            </span>
            {!userToggleable && (
              <Lock size={11} className="text-slate-400 flex-shrink-0" />
            )}
          </div>
          <p className="text-xs text-slate-400 mt-0.5 leading-relaxed">{description}</p>
          <p className="text-[10px] font-mono text-slate-300 mt-1">ID: {id}</p>
        </div>

        {/* Toggle */}
        {userToggleable ? (
          <button
            onClick={() => onToggle(!enabled)}
            aria-label={`${enabled ? 'Disable' : 'Enable'} ${label}`}
            className="flex-shrink-0 transition-colors"
          >
            {enabled ? (
              <ToggleRight size={28} className="text-[#004282]" />
            ) : (
              <ToggleLeft size={28} className="text-slate-300" />
            )}
          </button>
        ) : (
          <div className="flex-shrink-0 px-2 py-0.5 bg-slate-100 text-slate-400 text-[10px] rounded font-medium">
            env-locked
          </div>
        )}
      </div>
    </div>
  );
}

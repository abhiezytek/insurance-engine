/**
 * ModuleContext — central enable/disable registry for application modules.
 *
 * Priority order (highest wins):
 *   1. Per-module env var  VITE_MODULE_BI=false  (can only force-disable)
 *   2. VITE_ENABLED_MODULES=bi,ypyg,audit        (comma-separated allowlist)
 *   3. localStorage overrides (user runtime toggle)
 *   4. Default: all modules enabled
 */
import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react';

export type ModuleId = 'bi' | 'ypyg' | 'products' | 'upload' | 'audit';

export interface ModuleDefinition {
  id: ModuleId;
  label: string;
  description: string;
  /** false when a VITE_MODULE_<ID>=false env var is present — cannot be re-enabled at runtime */
  userToggleable: boolean;
  enabled: boolean;
}

const STORAGE_KEY = 'insurance_engine_module_overrides';

/** Parse VITE_ENABLED_MODULES. Returns null when the env var is absent (= all allowed). */
function getEnvAllowlist(): Set<ModuleId> | null {
  const raw = (import.meta.env.VITE_ENABLED_MODULES as string | undefined) ?? '';
  if (!raw.trim()) return null;
  return new Set(
    raw
      .split(',')
      .map(s => s.trim() as ModuleId)
      .filter(Boolean),
  );
}

/** Collect modules that are hard-disabled via  VITE_MODULE_<ID>=false|0. */
function getEnvForcedDisabled(): Set<ModuleId> {
  const disabled = new Set<ModuleId>();
  const ALL_IDS: ModuleId[] = ['bi', 'ypyg', 'products', 'upload', 'audit'];
  for (const id of ALL_IDS) {
    const key = `VITE_MODULE_${id.toUpperCase()}`;
    const val = (import.meta.env as Record<string, string>)[key];
    if (val === 'false' || val === '0') disabled.add(id);
  }
  return disabled;
}

function loadStoredOverrides(): Partial<Record<ModuleId, boolean>> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as Partial<Record<ModuleId, boolean>>) : {};
  } catch {
    return {};
  }
}

function saveStoredOverrides(overrides: Partial<Record<ModuleId, boolean>>) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(overrides));
  } catch {
    // ignore storage errors
  }
}

const MODULE_META: { id: ModuleId; label: string; description: string }[] = [
  {
    id: 'bi',
    label: 'Benefit Illustration',
    description:
      'Century Income yearly benefit illustration including GI, LI, GSV and maturity benefit.',
  },
  {
    id: 'ypyg',
    label: 'YPYG',
    description:
      'You Pay You Get — formula-driven calculation engine showing policy benefit outputs.',
  },
  {
    id: 'products',
    label: 'Products & Formulas',
    description:
      'Manage insurance products, versions, formula expressions, and input parameters.',
  },
  {
    id: 'upload',
    label: 'Bulk Upload',
    description: 'Batch-import formulas and parameters via Excel (.xlsx) or CSV files.',
  },
  {
    id: 'audit',
    label: 'Audit Log',
    description:
      'View system activity timeline including uploads, calculations, and change events.',
  },
];

function buildInitialModules(): ModuleDefinition[] {
  const allowlist = getEnvAllowlist();
  const forcedDisabled = getEnvForcedDisabled();
  const stored = loadStoredOverrides();

  return MODULE_META.map(meta => {
    const envDefault = allowlist ? allowlist.has(meta.id) : true;
    const isForcedOff = forcedDisabled.has(meta.id);
    const userOverride =
      !isForcedOff && meta.id in stored ? stored[meta.id] : undefined;
    const enabled = isForcedOff
      ? false
      : userOverride !== undefined
        ? userOverride
        : envDefault;

    return {
      ...meta,
      enabled,
      userToggleable: !isForcedOff,
    };
  });
}

// ---------------------------------------------------------------------------
// Context
// ---------------------------------------------------------------------------

interface ModuleContextValue {
  modules: ModuleDefinition[];
  isEnabled: (id: ModuleId) => boolean;
  toggleModule: (id: ModuleId, enabled: boolean) => void;
  enabledModules: ModuleDefinition[];
}

const ModuleContext = createContext<ModuleContextValue | null>(null);

export function ModuleProvider({ children }: { children: ReactNode }) {
  const [modules, setModules] = useState<ModuleDefinition[]>(buildInitialModules);

  const isEnabled = useCallback(
    (id: ModuleId) => modules.find(m => m.id === id)?.enabled ?? false,
    [modules],
  );

  const toggleModule = useCallback((id: ModuleId, enabled: boolean) => {
    setModules(prev => {
      const updated = prev.map(m =>
        m.id === id && m.userToggleable ? { ...m, enabled } : m,
      );
      // Persist only user-toggleable overrides
      const overrides: Partial<Record<ModuleId, boolean>> = {};
      for (const m of updated) {
        if (m.userToggleable) overrides[m.id] = m.enabled;
      }
      saveStoredOverrides(overrides);
      return updated;
    });
  }, []);

  const enabledModules = modules.filter(m => m.enabled);

  return (
    <ModuleContext.Provider value={{ modules, isEnabled, toggleModule, enabledModules }}>
      {children}
    </ModuleContext.Provider>
  );
}

export function useModules(): ModuleContextValue {
  const ctx = useContext(ModuleContext);
  if (!ctx) throw new Error('useModules must be used inside <ModuleProvider>');
  return ctx;
}

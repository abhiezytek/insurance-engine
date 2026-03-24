import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import { apiClient } from '../utils/apiClient';

export interface ModulePermission {
  moduleCode: string;
  moduleName: string;
  canView: boolean;
  canExecute: boolean;
  canApprove: boolean;
  canDownload: boolean;
  canUpload: boolean;
  canAdmin: boolean;
}

interface PermissionContextValue {
  permissions: ModulePermission[];
  isLoading: boolean;
  hasAccess: (moduleCode: string) => boolean;
  canView: (moduleCode: string) => boolean;
  canEdit: (moduleCode: string) => boolean;
  canAdmin: (moduleCode: string) => boolean;
}

const PermissionContext = createContext<PermissionContextValue | null>(null);

const STORAGE_KEY = 'precision_pro_auth';

function getStoredUserId(): string | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { userId?: number; username?: string };
    // Prefer numeric userId if stored; fall back to username for older auth payloads.
    if (parsed.userId != null) return String(parsed.userId);
    return parsed.username ?? null;
  } catch {
    return null;
  }
}

function findModule(permissions: ModulePermission[], moduleCode: string): ModulePermission | undefined {
  return permissions.find((p) => p.moduleCode === moduleCode);
}

// Default to full access when permissions cannot be loaded (backward compatibility).
function fullAccessDefaults(): ModulePermission[] {
  const modules = [
    { code: 'BI', name: 'Benefit Illustration' },
    { code: 'YPYG', name: 'YPYG' },
    { code: 'AUDIT', name: 'Audit' },
    { code: 'CONFIG', name: 'Configuration' },
    { code: 'USERMGMT', name: 'User Management' },
    { code: 'REPORTS', name: 'Reports' },
  ];
  return modules.map(({ code, name }) => ({
    moduleCode: code,
    moduleName: name,
    canView: true,
    canExecute: true,
    canApprove: true,
    canDownload: true,
    canUpload: true,
    canAdmin: true,
  }));
}

export function PermissionProvider({ children }: { children: ReactNode }) {
  const [permissions, setPermissions] = useState<ModulePermission[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function fetchPermissions() {
      const userId = getStoredUserId();
      if (!userId) {
        setPermissions(fullAccessDefaults());
        setIsLoading(false);
        return;
      }

      try {
        const res = await apiClient.get(`/api/usermgmt/access/${userId}`);
        if (!cancelled) {
          // The endpoint may return a direct array or a wrapper with a Permissions property.
          const data = res.data as ModulePermission[] | { permissions?: ModulePermission[] };
          const perms = Array.isArray(data) ? data : (data.permissions ?? []);
          setPermissions(perms);
        }
      } catch {
        // Graceful degradation: grant full access when the endpoint is unavailable.
        if (!cancelled) {
          setPermissions(fullAccessDefaults());
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    fetchPermissions();
    return () => { cancelled = true; };
  }, []);

  const hasAccess = useCallback(
    (moduleCode: string): boolean => {
      const mod = findModule(permissions, moduleCode);
      if (!mod) return false;
      return mod.canView || mod.canExecute || mod.canApprove || mod.canDownload || mod.canUpload || mod.canAdmin;
    },
    [permissions],
  );

  const canView = useCallback(
    (moduleCode: string): boolean => {
      const mod = findModule(permissions, moduleCode);
      return mod?.canView ?? false;
    },
    [permissions],
  );

  const canEdit = useCallback(
    (moduleCode: string): boolean => {
      const mod = findModule(permissions, moduleCode);
      return mod?.canExecute ?? false;
    },
    [permissions],
  );

  const canAdmin = useCallback(
    (moduleCode: string): boolean => {
      const mod = findModule(permissions, moduleCode);
      return mod?.canAdmin ?? false;
    },
    [permissions],
  );

  return (
    <PermissionContext.Provider value={{ permissions, isLoading, hasAccess, canView, canEdit, canAdmin }}>
      {children}
    </PermissionContext.Provider>
  );
}

export function usePermission(): PermissionContextValue {
  const ctx = useContext(PermissionContext);
  if (!ctx) throw new Error('usePermission must be used inside <PermissionProvider>');
  return ctx;
}

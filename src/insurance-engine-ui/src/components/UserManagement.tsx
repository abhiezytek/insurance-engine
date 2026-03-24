/**
 * UserManagement — standalone module for managing users, roles, module access,
 * and viewing audit logs.
 *
 * Tabs:
 *   1. Users          – CRUD for system users
 *   2. Roles          – CRUD for roles with predefined defaults
 *   3. Module Access  – Permission matrix (roles × modules)
 *   4. Audit Logs     – Searchable, paginated event history with CSV export
 */
import { useState, useEffect, useCallback } from 'react';
import {
  Users, Shield, Grid3X3, ScrollText, Plus, Edit3, Save,
  Search, Download, RefreshCw, AlertCircle, Check, XCircle, Key,
} from 'lucide-react';
import { apiClient } from '../utils/apiClient';

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------
interface UserApi {
  id: number;
  fullName: string;
  email: string;
  mobile?: string;
  employeeId?: string;
  department?: string;
  role?: string;
  status: string;
}

// Normalised form — optional API fields are coerced to empty strings for controlled inputs
interface User {
  id: number;
  fullName: string;
  email: string;
  mobile: string;
  employeeId: string;
  department: string;
  role: string;
  status: string;
}

interface Role {
  id: number;
  roleName: string;
  description: string;
  isActive: boolean;
}

interface AccessCell {
  roleId: number;
  module: string;
  permissions: Record<string, boolean>;
}

interface AuditLog {
  logId: number;
  eventType: string;
  module: string;
  user: string;
  timestamp: string;
  status: string;
}

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------
const TABS = [
  { id: 'users',   label: 'Users',         icon: Users },
  { id: 'roles',   label: 'Roles',         icon: Shield },
  { id: 'modules', label: 'Module Access',  icon: Grid3X3 },
  { id: 'audit',   label: 'Audit Logs',     icon: ScrollText },
] as const;

type TabId = (typeof TABS)[number]['id'];

const PREDEFINED_ROLES = [
  'Super Admin',
  'Admin',
  'Actuary / Product Manager',
  'Operations User',
  'Read Only / Viewer',
  'Audit User',
];

const MODULES = ['BI', 'YPYG', 'Audit', 'Config', 'UserMgmt', 'Reports'] as const;
const PERMISSION_KEYS = ['View', 'Execute', 'Approve', 'Download', 'Upload', 'Admin'] as const;

// Role badge color mapping
const ROLE_COLORS: Record<string, string> = {
  'super admin':               'bg-purple-100 text-purple-700',
  'admin':                     'bg-blue-100 text-blue-700',
  'actuary / product manager': 'bg-green-100 text-green-700',
  'actuary':                   'bg-green-100 text-green-700',
  'operations user':           'bg-amber-100 text-amber-700',
  'operations':                'bg-amber-100 text-amber-700',
  'read only / viewer':        'bg-slate-100 text-slate-600',
  'readonly':                  'bg-slate-100 text-slate-600',
  'audit user':                'bg-cyan-100 text-cyan-700',
  'auditor':                   'bg-cyan-100 text-cyan-700',
};

function RoleBadge({ role }: { role: string }) {
  if (!role) return <span className="text-slate-400">—</span>;
  const cls = ROLE_COLORS[role.toLowerCase()] ?? 'bg-slate-100 text-slate-600';
  return (
    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold ${cls}`}>
      {role}
    </span>
  );
}

const EMPTY_USER: Omit<User, 'id'> = {
  fullName: '', email: '', mobile: '', employeeId: '',
  department: '', role: '', status: 'Active',
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
async function safeFetch<T>(url: string, fallback: T): Promise<T> {
  try { return (await apiClient.get<T>(url)).data; }
  catch { return fallback; }
}

function exportCsv(rows: AuditLog[]) {
  if (rows.length === 0) return;
  const headers = ['Log ID', 'Event Type', 'Module', 'User', 'Timestamp', 'Status'];
  const csvRows = [
    headers.join(','),
    ...rows.map(r =>
      [r.logId, r.eventType, r.module, r.user, r.timestamp, r.status]
        .map(v => `"${String(v ?? '').replace(/"/g, '""')}"`)
        .join(','),
    ),
  ];
  const blob = new Blob([csvRows.join('\n')], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `audit-logs-${new Date().toISOString().slice(0, 10)}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}

// ---------------------------------------------------------------------------
// Shared UI primitives (same pattern as AdminMaster)
// ---------------------------------------------------------------------------
function Section({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  return (
    <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
      <div className="px-6 py-4 border-b border-slate-100 bg-blue-50/40">
        <h3 className="text-sm font-bold text-[#004282]">{title}</h3>
        {subtitle && <p className="text-xs text-slate-500 mt-0.5">{subtitle}</p>}
      </div>
      <div className="overflow-x-auto">{children}</div>
    </div>
  );
}

function THead({ cols }: { cols: string[] }) {
  return (
    <thead>
      <tr className="bg-[#004282] text-white text-xs uppercase tracking-wider">
        {cols.map(c => (
          <th key={c} className="px-3 py-2.5 text-center whitespace-nowrap">{c}</th>
        ))}
      </tr>
    </thead>
  );
}

function EmptyState({ message }: { message: string }) {
  return (
    <div className="text-center py-12 text-slate-400 text-sm flex flex-col items-center gap-2">
      <AlertCircle size={20} />
      {message}
    </div>
  );
}

// ---------------------------------------------------------------------------
// 1. Users Tab
// ---------------------------------------------------------------------------
function UsersTab({ users, roles, onReload }: { users: User[]; roles: Role[]; onReload: () => void }) {
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState(EMPTY_USER);
  const [searchTerm, setSearchTerm] = useState('');
  const set = (k: keyof typeof form, v: string) => setForm(prev => ({ ...prev, [k]: v }));

  const handleSave = async () => {
    try {
      if (editingId != null) {
        await apiClient.put(`/api/usermgmt/users/${editingId}`, form);
      } else {
        await apiClient.post('/api/usermgmt/users', form);
      }
      setCreating(false); setEditingId(null); setForm(EMPTY_USER);
      onReload();
    } catch { /* graceful — API may not exist yet */ }
  };

  const toggleStatus = async (u: User) => {
    try {
      if (u.status === 'Active') {
        await apiClient.delete(`/api/usermgmt/users/${u.id}`);
      } else {
        await apiClient.put(`/api/usermgmt/users/${u.id}`, { ...u, status: 'Active' });
      }
      onReload();
    } catch { /* graceful */ }
  };

  const resetPassword = async (u: User) => {
    try {
      await apiClient.put(`/api/usermgmt/users/${u.id}/reset-password`, {});
      onReload();
    } catch { /* graceful */ }
  };

  const startEdit = (u: User) => {
    setEditingId(u.id);
    setForm({
      fullName: u.fullName, email: u.email, mobile: u.mobile,
      employeeId: u.employeeId, department: u.department,
      role: u.role, status: u.status,
    });
    setCreating(true);
  };

  const filtered = users.filter(u => {
    if (!searchTerm) return true;
    const q = searchTerm.toLowerCase();
    return (
      u.fullName.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q) ||
      u.department.toLowerCase().includes(q) ||
      u.role.toLowerCase().includes(q)
    );
  });

  return (
    <Section title="User Management" subtitle="Create and manage system users.">
      <div className="px-4 pt-3 pb-2 flex items-center gap-3">
        <div className="relative flex-1 max-w-xs">
          <Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            placeholder="Search users…"
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
            className="w-full pl-8 pr-2 py-1.5 rounded border border-blue-300 text-xs focus:ring-1 focus:ring-[#007bff]"
          />
        </div>
        <button
          onClick={() => { setCreating(!creating); setEditingId(null); setForm(EMPTY_USER); }}
          className="ml-auto flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition"
        >
          <Plus size={13} /> {creating ? 'Cancel' : 'Create User'}
        </button>
      </div>

      {creating && (
        <div className="px-4 pb-4 grid grid-cols-2 md:grid-cols-3 gap-3">
          <input placeholder="Full Name *" value={form.fullName} onChange={e => set('fullName', e.target.value)}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <input placeholder="Email *" type="email" value={form.email} onChange={e => set('email', e.target.value)}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <input placeholder="Mobile" value={form.mobile} onChange={e => set('mobile', e.target.value)}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <input placeholder="Employee ID" value={form.employeeId} onChange={e => set('employeeId', e.target.value)}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <input placeholder="Department" value={form.department} onChange={e => set('department', e.target.value)}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <select value={form.role} onChange={e => set('role', e.target.value)}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]">
            <option value="">— Select Role —</option>
            {roles.map(r => <option key={r.id} value={r.roleName}>{r.roleName}</option>)}
          </select>
          <select value={form.status} onChange={e => set('status', e.target.value)}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]">
            <option>Active</option><option>Inactive</option>
          </select>
          <button onClick={handleSave}
            className="col-span-full w-fit flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-green-600 text-white rounded-lg hover:bg-green-700 transition">
            <Save size={13} /> {editingId != null ? 'Update' : 'Save'}
          </button>
        </div>
      )}

      {filtered.length === 0 ? <EmptyState message="No users available." /> : (
        <table className="w-full text-xs">
          <THead cols={['', 'Full Name', 'Email', 'Department', 'Role', 'Status', 'Actions']} />
          <tbody className="divide-y divide-slate-100">
            {filtered.map(u => {
              const initials = (u.fullName || '').split(' ').filter(w => w.length > 0).map(w => w[0]).join('').slice(0, 2).toUpperCase() || '?';
              const isInactive = u.status !== 'Active';
              return (
              <tr key={u.id} className={`hover:bg-blue-50/20 ${isInactive ? 'opacity-60' : ''} text-slate-700`}>
                <td className="px-3 py-2 text-center">
                  <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-[#004282] text-white text-xs font-bold">
                    {initials}
                  </span>
                </td>
                <td className={`px-3 py-2 text-center ${isInactive ? 'line-through text-slate-400' : ''}`}>{u.fullName}</td>
                <td className="px-3 py-2 text-center">{u.email}</td>
                <td className="px-3 py-2 text-center">{u.department || '—'}</td>
                <td className="px-3 py-2 text-center">
                  <RoleBadge role={u.role} />
                </td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                    ${u.status === 'Active' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>{u.status}</span>
                </td>
                <td className="px-3 py-2 text-center">
                  <div className="flex items-center justify-center gap-2">
                    <button onClick={() => startEdit(u)} className="text-[#004282] hover:text-[#007bff]" title="Edit">
                      <Edit3 size={13} />
                    </button>
                    <button onClick={() => resetPassword(u)} className="text-amber-600 hover:text-amber-800" title="Reset Password">
                      <Key size={13} />
                    </button>
                    <button onClick={() => toggleStatus(u)}
                      className={`text-xs font-semibold px-2 py-0.5 rounded ${u.status === 'Active' ? 'bg-red-100 text-red-700 hover:bg-red-200' : 'bg-green-100 text-green-700 hover:bg-green-200'}`}>
                      {u.status === 'Active' ? 'Deactivate' : 'Activate'}
                    </button>
                  </div>
                </td>
              </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </Section>
  );
}

// ---------------------------------------------------------------------------
// 2. Roles Tab
// ---------------------------------------------------------------------------
function RolesTab({ roles, onReload }: { roles: Role[]; onReload: () => void }) {
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState({ roleName: '', description: '', isActive: true });

  const handleSave = async () => {
    try {
      if (editingId != null) {
        await apiClient.put(`/api/usermgmt/roles/${editingId}`, form);
      } else {
        await apiClient.post('/api/usermgmt/roles', form);
      }
      setCreating(false); setEditingId(null); setForm({ roleName: '', description: '', isActive: true });
      onReload();
    } catch { /* graceful */ }
  };

  const toggleActive = async (r: Role) => {
    try {
      if (r.isActive) {
        await apiClient.delete(`/api/usermgmt/roles/${r.id}`);
      } else {
        await apiClient.put(`/api/usermgmt/roles/${r.id}`, { ...r, isActive: true });
      }
      onReload();
    } catch { /* graceful */ }
  };

  const startEdit = (r: Role) => {
    setEditingId(r.id);
    setForm({ roleName: r.roleName, description: r.description, isActive: r.isActive });
    setCreating(true);
  };

  // Merge API roles with predefined defaults for display
  const displayRoles: Role[] = roles.length > 0
    ? roles
    : PREDEFINED_ROLES.map((name, idx) => ({
        id: idx + 1, roleName: name, description: '', isActive: true,
      }));

  return (
    <Section title="Role Management" subtitle="Define roles for access control.">
      <div className="px-4 pt-3 pb-2 flex justify-end">
        <button
          onClick={() => { setCreating(!creating); setEditingId(null); setForm({ roleName: '', description: '', isActive: true }); }}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition"
        >
          <Plus size={13} /> {creating ? 'Cancel' : 'Create Role'}
        </button>
      </div>

      {creating && (
        <div className="px-4 pb-4 grid grid-cols-2 md:grid-cols-3 gap-3">
          <input placeholder="Role Name *" value={form.roleName} onChange={e => setForm(p => ({ ...p, roleName: e.target.value }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <input placeholder="Description" value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <select value={form.isActive ? 'Active' : 'Inactive'} onChange={e => setForm(p => ({ ...p, isActive: e.target.value === 'Active' }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]">
            <option>Active</option><option>Inactive</option>
          </select>
          <button onClick={handleSave}
            className="col-span-full w-fit flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-green-600 text-white rounded-lg hover:bg-green-700 transition">
            <Save size={13} /> {editingId != null ? 'Update' : 'Save'}
          </button>
        </div>
      )}

      {displayRoles.length === 0 ? <EmptyState message="No roles available." /> : (
        <table className="w-full text-xs">
          <THead cols={['ID', 'Role Name', 'Description', 'Status', 'Actions']} />
          <tbody className="divide-y divide-slate-100">
            {displayRoles.map(r => (
              <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 text-center font-semibold text-[#004282]">{r.id}</td>
                <td className="px-3 py-2 text-center">{r.roleName}</td>
                <td className="px-3 py-2 text-center">{r.description || '—'}</td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                    ${r.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                    {r.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-3 py-2 text-center">
                  <div className="flex items-center justify-center gap-2">
                    <button onClick={() => startEdit(r)} className="text-[#004282] hover:text-[#007bff]" title="Edit">
                      <Edit3 size={13} />
                    </button>
                    <button onClick={() => toggleActive(r)}
                      className={`text-xs font-semibold px-2 py-0.5 rounded ${r.isActive ? 'bg-red-100 text-red-700 hover:bg-red-200' : 'bg-green-100 text-green-700 hover:bg-green-200'}`}>
                      {r.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Section>
  );
}

// ---------------------------------------------------------------------------
// 3. Module Access Tab
// ---------------------------------------------------------------------------
function ModuleAccessTab({ matrix, roles, onReload }: { matrix: AccessCell[]; roles: Role[]; onReload: () => void }) {
  const [saving, setSaving] = useState(false);
  const [local, setLocal] = useState<AccessCell[]>([]);

  useEffect(() => { setLocal(matrix); }, [matrix]);

  // Build a lookup for quick access
  const cellKey = (roleId: number, mod: string) => `${roleId}::${mod}`;
  const cellMap = new Map(local.map(c => [cellKey(c.roleId, c.module), c]));

  const toggle = (roleId: number, mod: string, perm: string) => {
    setLocal(prev =>
      prev.map(c => {
        if (c.roleId !== roleId || c.module !== mod) return c;
        return { ...c, permissions: { ...c.permissions, [perm]: !c.permissions[perm] } };
      }),
    );
  };

  const saveAll = async () => {
    setSaving(true);
    try {
      await apiClient.put('/api/usermgmt/access-matrix', local);
      onReload();
    } catch { /* graceful */ }
    finally { setSaving(false); }
  };

  const activeRoles = roles.filter(r => r.isActive);

  // If the matrix is empty, build a skeleton from known roles/modules
  const displayRoles = activeRoles.length > 0
    ? activeRoles
    : PREDEFINED_ROLES.map((name, idx) => ({ id: idx + 1, roleName: name, description: '', isActive: true }));

  return (
    <Section title="Module Access" subtitle="Permission matrix — roles × modules.">
      <div className="px-4 pt-3 pb-2 flex justify-end">
        <button onClick={saveAll} disabled={saving}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-green-600 text-white rounded-lg hover:bg-green-700 transition disabled:opacity-50 disabled:cursor-not-allowed">
          <Save size={13} /> {saving ? 'Saving…' : 'Save Changes'}
        </button>
      </div>

      <table className="w-full text-xs">
        <thead>
          <tr className="bg-[#004282] text-white text-xs uppercase tracking-wider">
            <th className="px-3 py-2.5 text-left whitespace-nowrap">Role</th>
            {MODULES.map(m => (
              <th key={m} colSpan={PERMISSION_KEYS.length} className="px-1 py-2.5 text-center whitespace-nowrap border-l border-blue-300/40">
                {m}
              </th>
            ))}
          </tr>
          <tr className="bg-[#004282]/90 text-white text-[10px] uppercase tracking-wider">
            <th className="px-3 py-1" />
            {MODULES.map(m =>
              PERMISSION_KEYS.map(p => (
                <th key={`${m}-${p}`} className="px-1 py-1 text-center whitespace-nowrap">{p.slice(0, 4)}</th>
              )),
            )}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {displayRoles.map(r => (
            <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
              <td className="px-3 py-2 font-semibold text-[#004282] whitespace-nowrap">{r.roleName}</td>
              {MODULES.map(m =>
                PERMISSION_KEYS.map(p => {
                  const cell = cellMap.get(cellKey(r.id, m));
                  const checked = cell?.permissions[p] ?? false;
                  return (
                    <td key={`${m}-${p}`} className="px-1 py-2 text-center">
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() => toggle(r.id, m, p)}
                        className="accent-[#004282] cursor-pointer"
                      />
                    </td>
                  );
                }),
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// 4. Audit Logs Tab
// ---------------------------------------------------------------------------
function AuditLogsTab({ logs, hasMore, onLoadMore, onSearch }: {
  logs: AuditLog[];
  hasMore: boolean;
  onLoadMore: () => void;
  onSearch: (params: Record<string, string>) => void;
}) {
  const [filters, setFilters] = useState({ user: '', module: '', dateFrom: '', dateTo: '', action: '', status: '' });
  const setF = (k: keyof typeof filters, v: string) => setFilters(prev => ({ ...prev, [k]: v }));

  const applySearch = () => {
    const params: Record<string, string> = {};
    if (filters.user)     params.user     = filters.user;
    if (filters.module)   params.module   = filters.module;
    if (filters.dateFrom) params.dateFrom = filters.dateFrom;
    if (filters.dateTo)   params.dateTo   = filters.dateTo;
    if (filters.action)   params.action   = filters.action;
    if (filters.status)   params.status   = filters.status;
    onSearch(params);
  };

  return (
    <Section title="Audit Logs" subtitle="Searchable event history.">
      {/* Filters */}
      <div className="px-4 pt-3 pb-2 flex flex-wrap items-end gap-3">
        <input placeholder="User" value={filters.user} onChange={e => setF('user', e.target.value)}
          className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff] w-28" />
        <input placeholder="Module" value={filters.module} onChange={e => setF('module', e.target.value)}
          className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff] w-28" />
        <input type="date" title="Date From" value={filters.dateFrom} onChange={e => setF('dateFrom', e.target.value)}
          className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
        <input type="date" title="Date To" value={filters.dateTo} onChange={e => setF('dateTo', e.target.value)}
          className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
        <input placeholder="Action" value={filters.action} onChange={e => setF('action', e.target.value)}
          className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff] w-28" />
        <select value={filters.status} onChange={e => setF('status', e.target.value)}
          className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]">
          <option value="">All Status</option>
          <option value="Success">Success</option>
          <option value="Failure">Failure</option>
        </select>
        <button onClick={applySearch}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition">
          <Search size={13} /> Search
        </button>
        <button onClick={() => exportCsv(logs)}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-white text-[#004282] border border-[#004282] rounded-lg hover:bg-blue-50 transition">
          <Download size={13} /> Export CSV
        </button>
      </div>

      {logs.length === 0 ? <EmptyState message="No audit logs available." /> : (
        <table className="w-full text-xs">
          <THead cols={['Log ID', 'Event Type', 'Module', 'User', 'Timestamp', 'Status']} />
          <tbody className="divide-y divide-slate-100">
            {logs.map(l => (
              <tr key={l.logId} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 text-center font-semibold text-[#004282]">{l.logId}</td>
                <td className="px-3 py-2 text-center">
                  <span className="inline-block px-2 py-0.5 rounded-full text-xs font-semibold bg-blue-100 text-blue-700">{l.eventType}</span>
                </td>
                <td className="px-3 py-2 text-center">{l.module ?? '—'}</td>
                <td className="px-3 py-2 text-center">{l.user}</td>
                <td className="px-3 py-2 text-center">{new Date(l.timestamp).toLocaleString()}</td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold
                    ${l.status === 'Success' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                    {l.status === 'Success' ? <Check size={11} /> : <XCircle size={11} />}
                    {l.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {hasMore && (
        <div className="px-4 py-3 flex justify-center">
          <button onClick={onLoadMore}
            className="flex items-center gap-1.5 px-4 py-1.5 text-xs font-semibold bg-slate-100 text-slate-600 rounded-lg hover:bg-slate-200 transition">
            Load More
          </button>
        </div>
      )}
    </Section>
  );
}

// ---------------------------------------------------------------------------
// Main component
// ---------------------------------------------------------------------------
export default function UserManagement() {
  const [tab, setTab] = useState<TabId>('users');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [users, setUsers]       = useState<User[]>([]);
  const [roles, setRoles]       = useState<Role[]>([]);
  const [matrix, setMatrix]     = useState<AccessCell[]>([]);
  const [auditLogs, setAuditLogs]   = useState<AuditLog[]>([]);
  const [auditPage, setAuditPage]   = useState(1);
  const [auditHasMore, setAuditHasMore] = useState(true);

  // --- data loaders ---
  const loadUsers = useCallback(async () => {
    const raw = await safeFetch<UserApi[]>('/api/usermgmt/users', []);
    setUsers(raw.map(u => ({
      ...u,
      mobile: u.mobile ?? '',
      employeeId: u.employeeId ?? '',
      department: u.department ?? '',
      role: u.role ?? '',
    })));
  }, []);

  const loadRoles = useCallback(async () => {
    setRoles(await safeFetch<Role[]>('/api/usermgmt/roles', []));
  }, []);

  const loadMatrix = useCallback(async () => {
    setMatrix(await safeFetch<AccessCell[]>('/api/usermgmt/access-matrix', []));
  }, []);

  const loadAuditLogs = useCallback(async (page = 1, append = false, params: Record<string, string> = {}) => {
    const qs = new URLSearchParams({ page: String(page), pageSize: '20', ...params });
    const data = await safeFetch<AuditLog[]>(`/api/audit/logs?${qs.toString()}`, []);
    if (append) { setAuditLogs(prev => [...prev, ...data]); } else { setAuditLogs(data); }
    setAuditHasMore(data.length >= 20);
    setAuditPage(page);
  }, []);

  const loadAll = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      await Promise.all([loadUsers(), loadRoles(), loadMatrix(), loadAuditLogs()]);
    } catch {
      setError('Failed to load data. Some sections may be empty.');
    } finally { setLoading(false); }
  }, [loadUsers, loadRoles, loadMatrix, loadAuditLogs]);

  useEffect(() => { loadAll(); }, [loadAll]);

  return (
    <div className="space-y-6">
      {/* Page heading */}
      <div className="flex items-start justify-between">
        <div>
          <h2 className="text-2xl font-bold text-[#004282] flex items-center gap-2">
            <Users size={22} />
            User Management
            <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
          </h2>
          <p className="mt-2 text-slate-500 text-sm">
            Manage users, roles, module-level permissions, and view audit logs.
          </p>
        </div>
        <button
          onClick={loadAll}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-600 rounded-lg hover:bg-slate-200 transition"
        >
          <RefreshCw size={13} className={loading ? 'animate-spin' : ''} />
          Refresh
        </button>
      </div>

      {error && (
        <div className="flex items-start gap-2 p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700">
          <AlertCircle size={16} className="mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Tab bar */}
      <div className="flex flex-wrap gap-2">
        {TABS.map(t => {
          const Icon = t.icon;
          return (
            <button
              key={t.id}
              onClick={() => setTab(t.id)}
              className={`flex items-center gap-1.5 px-4 py-2 rounded-full text-sm font-semibold transition
                ${tab === t.id
                  ? 'bg-[#004282] text-white shadow-md'
                  : 'bg-white text-[#004282] border border-[#004282] hover:bg-blue-50'}`}
            >
              <Icon size={14} />
              {t.label}
            </button>
          );
        })}
      </div>

      {/* Tab content */}
      {loading && (
        <div className="text-center py-12 text-slate-400 text-sm">Loading…</div>
      )}

      {!loading && tab === 'users'   && <UsersTab users={users} roles={roles} onReload={() => { loadUsers(); loadRoles(); }} />}
      {!loading && tab === 'roles'   && <RolesTab roles={roles} onReload={loadRoles} />}
      {!loading && tab === 'modules' && <ModuleAccessTab matrix={matrix} roles={roles} onReload={() => { loadMatrix(); loadRoles(); }} />}
      {!loading && tab === 'audit'   && (
        <AuditLogsTab
          logs={auditLogs}
          hasMore={auditHasMore}
          onLoadMore={() => loadAuditLogs(auditPage + 1, true)}
          onSearch={(params) => loadAuditLogs(1, false, params)}
        />
      )}
    </div>
  );
}

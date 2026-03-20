/**
 * AdminMaster — view and edit calculation formulas and factor tables.
 *
 * Tables displayed:
 *   1. GMB Factors      (Endowment: lookup by PPT × PT × AgeRange × Option)
 *   2. GSV Factors      (Endowment: lookup by PPT × PolicyYear)
 *   3. SSV Factors      (Endowment: lookup by PPT × PolicyYear)
 *   4. ULIP Charges     (PAC %, FMC %, Policy Admin monthly)
 *   5. Mortality Rates  (Gender × Age)
 *   6. Loyalty Factors  (Endowment Twin Income payout years)
 *
 * Editing is done inline.  Changes are persisted via the /api/admin/* endpoints.
 */
import { useState, useEffect, useCallback } from 'react';
import { Settings, RefreshCw, AlertCircle, Edit3, Save, X, Info, Plus } from 'lucide-react';
import { api } from '../api';

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------
interface GmbFactor  { id: number; ppt: number; pt: number; entryAgeMin: number; entryAgeMax: number; option: string; factor: number; }
interface GsvFactor  { id: number; ppt: number; policyYear: number; factorPercent: number; }
interface SsvFactor  { id: number; ppt: number; policyYear: number; factor1: number; factor2: number; }
interface UlipCharge { id: number; productId: number; chargeType: string; chargeValue: number; chargeFrequency: string; }
interface MortalityRate { id: number; gender: string; age: number; rate: number; }
interface LoyaltyFactor { id: number; ppt: number; policyYearFrom: number; policyYearTo?: number; ratePercent: number; }

interface AdminUser { id: number; fullName: string; email: string; mobile?: string; employeeId?: string; department?: string; status: string; }
interface AdminRole { id: number; roleName: string; description: string; isActive: boolean; }
interface ModuleAccess { moduleId: number; moduleName: string; subModules: SubModuleAccess[]; }
interface SubModuleAccess { subModuleId: number; subModuleName: string; permissions: Record<string, boolean>; }
interface IntegrationConfig { id: number; name: string; baseUrl: string; authType: string; timeout: number; mockMode: boolean; isActive: boolean; }
interface AuditLog { logId: number; eventType: string; caseId?: string; doneBy: string; doneAt: string; }

// ---------------------------------------------------------------------------
// Helper: generic editable table
// ---------------------------------------------------------------------------
function EditableNumber({
  value,
  onSave,
}: {
  value: number;
  onSave: (v: number) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value);

  if (!editing)
    return (
      <span
        className="cursor-pointer text-[#004282] hover:underline flex items-center gap-1"
        title="Click to edit"
        onClick={() => { setDraft(value); setEditing(true); }}
      >
        {value}
        <Edit3 size={11} className="opacity-40" />
      </span>
    );

  return (
    <span className="flex items-center gap-1">
      <input
        type="number"
        step="any"
        value={draft}
        onChange={e => setDraft(parseFloat(e.target.value))}
        className="w-24 rounded border border-blue-300 px-1 py-0.5 text-xs focus:ring-1 focus:ring-[#007bff]"
        autoFocus
      />
      <button onClick={() => { onSave(draft); setEditing(false); }} title="Save"
        className="text-green-600 hover:text-green-800"><Save size={13} /></button>
      <button onClick={() => setEditing(false)} title="Cancel"
        className="text-red-500 hover:text-red-700"><X size={13} /></button>
    </span>
  );
}

// ---------------------------------------------------------------------------
// Section card wrapper
// ---------------------------------------------------------------------------
function Section({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
}) {
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

// ---------------------------------------------------------------------------
// GMB Factors table
// ---------------------------------------------------------------------------
function GmbTable({ rows, onUpdate }: { rows: GmbFactor[]; onUpdate: (id: number, factor: number) => void }) {
  return (
    <Section
      title="GMB Factors — Guaranteed Maturity Benefit"
      subtitle="Lookup by PPT × PT × Age Range × Option. Factor = GMB / Annual Premium."
    >
      <table className="w-full text-xs">
        <THead cols={['PPT', 'PT', 'Age Min', 'Age Max', 'Option', 'Factor']} />
        <tbody className="divide-y divide-slate-100">
          {rows.map(r => (
            <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
              <td className="px-3 py-2 text-center font-semibold text-[#004282]">{r.ppt}</td>
              <td className="px-3 py-2 text-center">{r.pt}</td>
              <td className="px-3 py-2 text-center">{r.entryAgeMin}</td>
              <td className="px-3 py-2 text-center">{r.entryAgeMax}</td>
              <td className="px-3 py-2 text-center">
                <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                  ${r.option === 'Twin' ? 'bg-purple-100 text-purple-700' :
                    r.option === 'Deferred' ? 'bg-amber-100 text-amber-700' :
                    'bg-green-100 text-green-700'}`}>
                  {r.option}
                </span>
              </td>
              <td className="px-3 py-2 text-center">
                <EditableNumber value={r.factor} onSave={v => onUpdate(r.id, v)} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// GSV Factors table
// ---------------------------------------------------------------------------
function GsvTable({ rows, onUpdate }: { rows: GsvFactor[]; onUpdate: (id: number, pct: number) => void }) {
  const ppts = [...new Set(rows.map(r => r.ppt))].sort((a, b) => a - b);
  return (
    <Section
      title="GSV Factors — Guaranteed Surrender Value"
      subtitle="GSV = Total Premiums Paid × (Factor / 100). Grouped by PPT."
    >
      <table className="w-full text-xs">
        <THead cols={['PPT', 'Policy Year', 'Factor (%)']} />
        <tbody className="divide-y divide-slate-100">
          {ppts.flatMap(ppt =>
            rows.filter(r => r.ppt === ppt).sort((a, b) => a.policyYear - b.policyYear).map((r, i) => (
              <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 text-center font-semibold text-[#004282]">{i === 0 ? ppt : ''}</td>
                <td className="px-3 py-2 text-center">{r.policyYear}</td>
                <td className="px-3 py-2 text-center">
                  <EditableNumber value={r.factorPercent} onSave={v => onUpdate(r.id, v)} />
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// SSV Factors table
// ---------------------------------------------------------------------------
function SsvTable({ rows, onUpdate }: { rows: SsvFactor[]; onUpdate: (id: number, f1: number, f2: number) => void }) {
  const ppts = [...new Set(rows.map(r => r.ppt))].sort((a, b) => a - b);
  return (
    <Section
      title="SSV Factors — Special Surrender Value"
      subtitle="SSV = (F1/100 × PaidUpGMB) + (F2/100 × IncomeComponent)"
    >
      <table className="w-full text-xs">
        <THead cols={['PPT', 'Policy Year', 'F1 (% × GMB)', 'F2 (% × Income)']} />
        <tbody className="divide-y divide-slate-100">
          {ppts.flatMap(ppt =>
            rows.filter(r => r.ppt === ppt).sort((a, b) => a.policyYear - b.policyYear).map((r, i) => (
              <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 text-center font-semibold text-[#004282]">{i === 0 ? ppt : ''}</td>
                <td className="px-3 py-2 text-center">{r.policyYear}</td>
                <td className="px-3 py-2 text-center">
                  <EditableNumber value={r.factor1} onSave={v => onUpdate(r.id, v, r.factor2)} />
                </td>
                <td className="px-3 py-2 text-center">
                  <EditableNumber value={r.factor2} onSave={v => onUpdate(r.id, r.factor1, v)} />
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// ULIP Charges table
// ---------------------------------------------------------------------------
function UlipChargesTable({ rows, onUpdate }: { rows: UlipCharge[]; onUpdate: (id: number, val: number) => void }) {
  const descriptions: Record<string, string> = {
    PremiumAllocation: 'PAC — Premium Allocation Charge (% of AP). Product spec: 0%.',
    FMC: 'Fund Management Charge (% of FV p.a.). Product spec: 1.35%.',
    PolicyAdmin: 'Policy Administration Charge (₹/month). Applied for first 10 years. Product spec: ₹100/month.',
  };
  return (
    <Section
      title="ULIP Charges"
      subtitle="Charges applied in ULIP Benefit Illustration calculations."
    >
      <table className="w-full text-xs">
        <THead cols={['Charge Type', 'Value', 'Frequency', 'Description']} />
        <tbody className="divide-y divide-slate-100">
          {rows.map(r => (
            <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
              <td className="px-3 py-2 font-semibold text-[#004282]">{r.chargeType}</td>
              <td className="px-3 py-2 text-center">
                <EditableNumber value={r.chargeValue} onSave={v => onUpdate(r.id, v)} />
              </td>
              <td className="px-3 py-2 text-center">{r.chargeFrequency}</td>
              <td className="px-3 py-2 text-slate-500 text-xs max-w-xs">{descriptions[r.chargeType] ?? ''}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// Mortality Rates table
// ---------------------------------------------------------------------------
function MortalityTable({ rows, onUpdate }: { rows: MortalityRate[]; onUpdate: (id: number, rate: number) => void }) {
  const [gender, setGender] = useState<'Male' | 'Female'>('Male');
  const filtered = rows.filter(r => r.gender === gender).sort((a, b) => a.age - b.age);
  return (
    <Section
      title="Mortality Rates — per ₹1,000 Sum At Risk (SAR)"
      subtitle="MC = (SAR × Rate) / 1000. Looked up by Gender × Age."
    >
      <div className="flex gap-2 px-4 pt-3 pb-1">
        {(['Male', 'Female'] as const).map(g => (
          <button key={g} onClick={() => setGender(g)}
            className={`px-3 py-1 rounded-full text-xs font-semibold transition
              ${gender === g ? 'bg-[#004282] text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>
            {g}
          </button>
        ))}
      </div>
      <table className="w-full text-xs">
        <THead cols={['Age', 'Rate (per ₹1,000 SAR)']} />
        <tbody className="divide-y divide-slate-100">
          {filtered.map(r => (
            <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
              <td className="px-3 py-2 text-center font-semibold text-[#004282]">{r.age}</td>
              <td className="px-3 py-2 text-center">
                <EditableNumber value={r.rate} onSave={v => onUpdate(r.id, v)} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// Loyalty Factors table
// ---------------------------------------------------------------------------
function LoyaltyTable({ rows, onUpdate }: { rows: LoyaltyFactor[]; onUpdate: (id: number, rate: number) => void }) {
  const ppts = [...new Set(rows.map(r => r.ppt))].sort((a, b) => a - b);
  return (
    <Section
      title="Loyalty Income Factors (Endowment)"
      subtitle="LI = AP × (Rate / 100). Rate > 0 only in Loyalty Income payout years."
    >
      <table className="w-full text-xs">
        <THead cols={['PPT', 'Year From', 'Year To', 'Rate (%)']} />
        <tbody className="divide-y divide-slate-100">
          {ppts.flatMap(ppt =>
            rows.filter(r => r.ppt === ppt).sort((a, b) => a.policyYearFrom - b.policyYearFrom).map((r, i) => (
              <tr key={r.id} className={`hover:bg-blue-50/20 text-slate-700 ${r.ratePercent > 0 ? 'bg-green-50/30' : ''}`}>
                <td className="px-3 py-2 text-center font-semibold text-[#004282]">{i === 0 ? ppt : ''}</td>
                <td className="px-3 py-2 text-center">{r.policyYearFrom}</td>
                <td className="px-3 py-2 text-center">{r.policyYearTo ?? '∞'}</td>
                <td className="px-3 py-2 text-center">
                  <EditableNumber value={r.ratePercent} onSave={v => onUpdate(r.id, v)} />
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// Graceful API helper — returns fallback on any error (e.g. 404)
// ---------------------------------------------------------------------------
async function safeFetch<T>(url: string, fallback: T, onError?: (msg: string) => void): Promise<T> {
  try { return (await api.get<T>(url)).data; }
  catch (e: any) {
    const status = e?.response?.status;
    const msg = e?.response?.data?.error || e?.message || 'Unknown error';
    const composed = `${url} → ${status ? `HTTP ${status}: ` : ''}${msg}`;
    console.error('Admin safeFetch failed', composed);
    onError?.(composed);
    return fallback;
  }
}

// ---------------------------------------------------------------------------
// Empty state
// ---------------------------------------------------------------------------
function EmptyState({ message }: { message: string }) {
  return (
    <div className="text-center py-12 text-slate-400 text-sm flex flex-col items-center gap-2">
      <AlertCircle size={20} />
      {message}
    </div>
  );
}

// ---------------------------------------------------------------------------
// User Management Tab
// ---------------------------------------------------------------------------
const EMPTY_USER: Omit<AdminUser, 'id'> = { fullName: '', email: '', mobile: '', employeeId: '', department: '', status: 'Active' };

function UserManagementTab({ users, onReload }: { users: AdminUser[]; onReload: () => void }) {
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState(EMPTY_USER);
  const set = (k: keyof typeof form, v: string) => setForm(prev => ({ ...prev, [k]: v }));

  const handleSave = async () => {
    try {
      if (editingId != null) {
        await api.put(`/api/admin/users/${editingId}`, form);
      } else {
        await api.post('/api/admin/users', form);
      }
      setCreating(false); setEditingId(null); setForm(EMPTY_USER);
      onReload();
    } catch { /* graceful — API may not exist yet */ }
  };

  const toggleStatus = async (u: AdminUser) => {
    try {
      await api.put(`/api/admin/users/${u.id}`, { ...u, status: u.status === 'Active' ? 'Inactive' : 'Active' });
      onReload();
    } catch { /* graceful */ }
  };

  const startEdit = (u: AdminUser) => {
    setEditingId(u.id);
    setForm({ fullName: u.fullName, email: u.email, mobile: u.mobile ?? '', employeeId: u.employeeId ?? '', department: u.department ?? '', status: u.status });
    setCreating(true);
  };

  return (
    <Section title="User Management" subtitle="Create and manage system users.">
      <div className="px-4 pt-3 pb-2 flex justify-end">
        <button onClick={() => { setCreating(!creating); setEditingId(null); setForm(EMPTY_USER); }}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition">
          <Plus size={13} /> {creating ? 'Cancel' : 'Create User'}
        </button>
      </div>

      {creating && (
        <div className="px-4 pb-4 grid grid-cols-2 md:grid-cols-3 gap-3">
          {(['fullName', 'email', 'mobile', 'employeeId', 'department'] as const).map(k => (
            <input key={k} placeholder={k.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase())}
              value={form[k] ?? ''} onChange={e => set(k, e.target.value)}
              className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          ))}
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

      {users.length === 0 ? <EmptyState message="No users available." /> : (
        <table className="w-full text-xs">
          <THead cols={['ID', 'Full Name', 'Email', 'Department', 'Status', 'Actions']} />
          <tbody className="divide-y divide-slate-100">
            {users.map(u => (
              <tr key={u.id} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 text-center font-semibold text-[#004282]">{u.id}</td>
                <td className="px-3 py-2 text-center">{u.fullName}</td>
                <td className="px-3 py-2 text-center">{u.email}</td>
                <td className="px-3 py-2 text-center">{u.department ?? '—'}</td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                    ${u.status === 'Active' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>{u.status}</span>
                </td>
                <td className="px-3 py-2 text-center flex items-center justify-center gap-2">
                  <button onClick={() => startEdit(u)} className="text-[#004282] hover:text-[#007bff]" title="Edit"><Edit3 size={13} /></button>
                  <button onClick={() => toggleStatus(u)}
                    className={`text-xs font-semibold px-2 py-0.5 rounded ${u.status === 'Active' ? 'bg-red-100 text-red-700 hover:bg-red-200' : 'bg-green-100 text-green-700 hover:bg-green-200'}`}>
                    {u.status === 'Active' ? 'Deactivate' : 'Activate'}
                  </button>
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
// Role Management Tab
// ---------------------------------------------------------------------------
function RoleManagementTab({ roles, onReload }: { roles: AdminRole[]; onReload: () => void }) {
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState({ roleName: '', description: '' });

  const handleSave = async () => {
    try {
      if (editingId != null) {
        await api.put(`/api/admin/roles/${editingId}`, form);
      } else {
        await api.post('/api/admin/roles', form);
      }
      setCreating(false); setEditingId(null); setForm({ roleName: '', description: '' });
      onReload();
    } catch { /* graceful */ }
  };

  const toggleActive = async (r: AdminRole) => {
    try {
      await api.put(`/api/admin/roles/${r.id}`, { ...r, isActive: !r.isActive });
      onReload();
    } catch { /* graceful */ }
  };

  return (
    <Section title="Role Management" subtitle="Define roles for access control.">
      <div className="px-4 pt-3 pb-2 flex justify-end">
        <button onClick={() => { setCreating(!creating); setEditingId(null); setForm({ roleName: '', description: '' }); }}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition">
          <Plus size={13} /> {creating ? 'Cancel' : 'Create Role'}
        </button>
      </div>

      {creating && (
        <div className="px-4 pb-4 grid grid-cols-2 gap-3">
          <input placeholder="Role Name" value={form.roleName} onChange={e => setForm(p => ({ ...p, roleName: e.target.value }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <input placeholder="Description" value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <button onClick={handleSave}
            className="col-span-full w-fit flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-green-600 text-white rounded-lg hover:bg-green-700 transition">
            <Save size={13} /> {editingId != null ? 'Update' : 'Save'}
          </button>
        </div>
      )}

      {roles.length === 0 ? <EmptyState message="No roles available." /> : (
        <table className="w-full text-xs">
          <THead cols={['ID', 'Role Name', 'Description', 'Status', 'Actions']} />
          <tbody className="divide-y divide-slate-100">
            {roles.map(r => (
              <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 text-center font-semibold text-[#004282]">{r.id}</td>
                <td className="px-3 py-2 text-center">{r.roleName}</td>
                <td className="px-3 py-2 text-center">{r.description}</td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                    ${r.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>{r.isActive ? 'Active' : 'Inactive'}</span>
                </td>
                <td className="px-3 py-2 text-center flex items-center justify-center gap-2">
                  <button onClick={() => { setEditingId(r.id); setForm({ roleName: r.roleName, description: r.description }); setCreating(true); }}
                    className="text-[#004282] hover:text-[#007bff]" title="Edit"><Edit3 size={13} /></button>
                  <button onClick={() => toggleActive(r)}
                    className={`text-xs font-semibold px-2 py-0.5 rounded ${r.isActive ? 'bg-red-100 text-red-700 hover:bg-red-200' : 'bg-green-100 text-green-700 hover:bg-green-200'}`}>
                    {r.isActive ? 'Deactivate' : 'Activate'}
                  </button>
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
// Module Access Tab
// ---------------------------------------------------------------------------
const PERMISSION_KEYS = ['View', 'Execute', 'Approve', 'Download', 'Upload', 'Admin'] as const;

function ModuleAccessTab({ modules, roles, onReload }: { modules: ModuleAccess[]; roles: AdminRole[]; onReload: () => void }) {
  const [selectedRole, setSelectedRole] = useState<number | ''>('');

  const togglePerm = async (subModuleId: number, perm: string, current: boolean) => {
    if (selectedRole === '') return;
    try {
      await api.put(`/api/admin/modules/access`, { roleId: selectedRole, subModuleId, permission: perm, granted: !current });
      onReload();
    } catch { /* graceful */ }
  };

  if (modules.length === 0) return <Section title="Module Access" subtitle="Assign module permissions to roles."><EmptyState message="No modules available." /></Section>;

  return (
    <Section title="Module Access" subtitle="Assign module permissions to roles.">
      <div className="px-4 pt-3 pb-2 flex items-center gap-3">
        <label className="text-xs font-semibold text-[#004282]">Role:</label>
        <select value={selectedRole} onChange={e => setSelectedRole(e.target.value ? Number(e.target.value) : '')}
          className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]">
          <option value="">— Select Role —</option>
          {roles.map(r => <option key={r.id} value={r.id}>{r.roleName}</option>)}
        </select>
      </div>

      <table className="w-full text-xs">
        <THead cols={['Module', 'Sub-Module', ...PERMISSION_KEYS]} />
        <tbody className="divide-y divide-slate-100">
          {modules.flatMap(m =>
            m.subModules.map((sm, idx) => (
              <tr key={sm.subModuleId} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 font-semibold text-[#004282]">{idx === 0 ? m.moduleName : ''}</td>
                <td className="px-3 py-2">{sm.subModuleName}</td>
                {PERMISSION_KEYS.map(p => (
                  <td key={p} className="px-3 py-2 text-center">
                    <input type="checkbox" checked={!!sm.permissions[p]} disabled={selectedRole === ''}
                      onChange={() => togglePerm(sm.subModuleId, p, !!sm.permissions[p])}
                      className="accent-[#004282] cursor-pointer disabled:cursor-not-allowed" />
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </Section>
  );
}

// ---------------------------------------------------------------------------
// Integration Config Tab
// ---------------------------------------------------------------------------
const EMPTY_INTEGRATION: Omit<IntegrationConfig, 'id'> = { name: '', baseUrl: '', authType: 'Bearer', timeout: 30, mockMode: false, isActive: true };

function IntegrationConfigTab({ configs, onReload }: { configs: IntegrationConfig[]; onReload: () => void }) {
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState(EMPTY_INTEGRATION);

  const handleSave = async () => {
    try {
      if (editingId != null) {
        await api.put(`/api/admin/integrations/${editingId}`, form);
      } else {
        await api.post('/api/admin/integrations', form);
      }
      setCreating(false); setEditingId(null); setForm(EMPTY_INTEGRATION);
      onReload();
    } catch { /* graceful */ }
  };

  const toggleMock = async (c: IntegrationConfig) => {
    try {
      await api.put(`/api/admin/integrations/${c.id}`, { ...c, mockMode: !c.mockMode });
      onReload();
    } catch { /* graceful */ }
  };

  const startEdit = (c: IntegrationConfig) => {
    setEditingId(c.id);
    setForm({ name: c.name, baseUrl: c.baseUrl, authType: c.authType, timeout: c.timeout, mockMode: c.mockMode, isActive: c.isActive });
    setCreating(true);
  };

  return (
    <Section title="Integration Config" subtitle="Manage external service integrations.">
      <div className="px-4 pt-3 pb-2 flex justify-end">
        <button onClick={() => { setCreating(!creating); setEditingId(null); setForm(EMPTY_INTEGRATION); }}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition">
          <Plus size={13} /> {creating ? 'Cancel' : 'Add Integration'}
        </button>
      </div>

      {creating && (
        <div className="px-4 pb-4 grid grid-cols-2 md:grid-cols-3 gap-3">
          <input placeholder="Name" value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <input placeholder="Base URL" value={form.baseUrl} onChange={e => setForm(p => ({ ...p, baseUrl: e.target.value }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <select value={form.authType} onChange={e => setForm(p => ({ ...p, authType: e.target.value }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]">
            <option>Bearer</option><option>Basic</option><option>ApiKey</option><option>None</option>
          </select>
          <input type="number" placeholder="Timeout (s)" value={form.timeout} onChange={e => setForm(p => ({ ...p, timeout: Number(e.target.value) }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <label className="flex items-center gap-2 text-xs">
            <input type="checkbox" checked={form.mockMode} onChange={e => setForm(p => ({ ...p, mockMode: e.target.checked }))} className="accent-[#004282]" />
            Mock Mode
          </label>
          <label className="flex items-center gap-2 text-xs">
            <input type="checkbox" checked={form.isActive} onChange={e => setForm(p => ({ ...p, isActive: e.target.checked }))} className="accent-[#004282]" />
            Active
          </label>
          <button onClick={handleSave}
            className="col-span-full w-fit flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-green-600 text-white rounded-lg hover:bg-green-700 transition">
            <Save size={13} /> {editingId != null ? 'Update' : 'Save'}
          </button>
        </div>
      )}

      {configs.length === 0 ? <EmptyState message="No integrations configured." /> : (
        <table className="w-full text-xs">
          <THead cols={['Name', 'Base URL', 'Auth Type', 'Timeout', 'Mock Mode', 'Active', 'Actions']} />
          <tbody className="divide-y divide-slate-100">
            {configs.map(c => (
              <tr key={c.id} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 font-semibold text-[#004282]">{c.name}</td>
                <td className="px-3 py-2 text-center font-mono text-xs">{c.baseUrl}</td>
                <td className="px-3 py-2 text-center">{c.authType}</td>
                <td className="px-3 py-2 text-center">{c.timeout}s</td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                    ${c.mockMode ? 'bg-amber-100 text-amber-700' : 'bg-green-100 text-green-700'}`}>{c.mockMode ? 'Mock' : 'Live'}</span>
                </td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                    ${c.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>{c.isActive ? 'Yes' : 'No'}</span>
                </td>
                <td className="px-3 py-2 text-center flex items-center justify-center gap-2">
                  <button onClick={() => startEdit(c)} className="text-[#004282] hover:text-[#007bff]" title="Edit"><Edit3 size={13} /></button>
                  <button onClick={() => toggleMock(c)}
                    className={`text-xs font-semibold px-2 py-0.5 rounded ${c.mockMode ? 'bg-green-100 text-green-700 hover:bg-green-200' : 'bg-amber-100 text-amber-700 hover:bg-amber-200'}`}>
                    {c.mockMode ? 'Go Live' : 'Mock'}
                  </button>
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
// Audit Logs Tab
// ---------------------------------------------------------------------------
function AuditLogsTab({ logs, hasMore, onLoadMore }: { logs: AuditLog[]; hasMore: boolean; onLoadMore: () => void }) {
  if (logs.length === 0) return <Section title="Audit Logs" subtitle="Searchable event history."><EmptyState message="No audit logs available." /></Section>;
  return (
    <Section title="Audit Logs" subtitle="Searchable event history.">
      <table className="w-full text-xs">
        <THead cols={['Log ID', 'Event Type', 'Case ID', 'Done By', 'Done At']} />
        <tbody className="divide-y divide-slate-100">
          {logs.map(l => (
            <tr key={l.logId} className="hover:bg-blue-50/20 text-slate-700">
              <td className="px-3 py-2 text-center font-semibold text-[#004282]">{l.logId}</td>
              <td className="px-3 py-2 text-center">
                <span className="inline-block px-2 py-0.5 rounded-full text-xs font-semibold bg-blue-100 text-blue-700">{l.eventType}</span>
              </td>
              <td className="px-3 py-2 text-center">{l.caseId ?? '—'}</td>
              <td className="px-3 py-2 text-center">{l.doneBy}</td>
              <td className="px-3 py-2 text-center">{new Date(l.doneAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
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
// Formula reference card
// ---------------------------------------------------------------------------
function FormulaCard({ title, items }: { title: string; items: string[] }) {
  return (
    <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5">
      <h4 className="text-sm font-bold text-[#004282] mb-3 flex items-center gap-2">
        <Info size={14} /> {title}
      </h4>
      <ul className="space-y-1.5">
        {items.map(item => (
          <li key={item} className="text-xs text-slate-600 font-mono bg-slate-50 px-3 py-1.5 rounded-lg">
            {item}
          </li>
        ))}
      </ul>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab selector
// ---------------------------------------------------------------------------
const TABS = [
  { id: 'formulas',      label: 'Formula Reference' },
  { id: 'gmb',           label: 'GMB Factors' },
  { id: 'gsv',           label: 'GSV Factors' },
  { id: 'ssv',           label: 'SSV Factors' },
  { id: 'ulip',          label: 'ULIP Charges' },
  { id: 'mortality',     label: 'Mortality Rates' },
  { id: 'loyalty',       label: 'Loyalty Factors' },
  { id: 'users',         label: 'User Management' },
  { id: 'roles',         label: 'Role Management' },
  { id: 'modules',       label: 'Module Access' },
  { id: 'integrations',  label: 'Integration Config' },
  { id: 'audit',         label: 'Audit Logs' },
] as const;

type TabId = (typeof TABS)[number]['id'];

// ---------------------------------------------------------------------------
// Main component
// ---------------------------------------------------------------------------
export default function AdminMaster() {
  const [tab, setTab]               = useState<TabId>('formulas');
  const [loading, setLoading]       = useState(false);
  const [error, setError]           = useState<string | null>(null);

  const [gmbRows,       setGmbRows]       = useState<GmbFactor[]>([]);
  const [gsvRows,       setGsvRows]       = useState<GsvFactor[]>([]);
  const [ssvRows,       setSsvRows]       = useState<SsvFactor[]>([]);
  const [ulipRows,      setUlipRows]      = useState<UlipCharge[]>([]);
  const [mortalityRows, setMortalityRows] = useState<MortalityRate[]>([]);
  const [loyaltyRows,   setLoyaltyRows]   = useState<LoyaltyFactor[]>([]);

  // New tab state
  const [adminUsers,      setAdminUsers]      = useState<AdminUser[]>([]);
  const [adminRoles,      setAdminRoles]      = useState<AdminRole[]>([]);
  const [modules,         setModules]         = useState<ModuleAccess[]>([]);
  const [integrations,    setIntegrations]    = useState<IntegrationConfig[]>([]);
  const [auditLogs,       setAuditLogs]       = useState<AuditLog[]>([]);
  const [auditPage,       setAuditPage]       = useState(1);
  const [auditHasMore,    setAuditHasMore]    = useState(true);
  const [productScope,    setProductScope]    = useState<string>('');
  const [versionScope,    setVersionScope]    = useState<string>('');
  const [products,        setProducts]        = useState<{ code: string; name: string }[]>([]);

  // --- loaders for new tabs (graceful on 404) ---
  const scoped = useCallback((path: string) => {
    const params = new URLSearchParams();
    if (productScope) params.append('productCode', productScope);
    if (versionScope) params.append('version', versionScope);
    const qs = params.toString();
    return qs ? `${path}?${qs}` : path;
  }, [productScope, versionScope]);

  const loadUsers        = useCallback(async () => { setAdminUsers(await safeFetch<AdminUser[]>('/api/admin/users', [], setError)); }, [setError]);
  const loadRoles        = useCallback(async () => { setAdminRoles(await safeFetch<AdminRole[]>('/api/admin/roles', [], setError)); }, [setError]);
  const loadModules      = useCallback(async () => { setModules(await safeFetch<ModuleAccess[]>('/api/admin/modules', [], setError)); }, [setError]);
  const loadIntegrations = useCallback(async () => { setIntegrations(await safeFetch<IntegrationConfig[]>('/api/admin/integrations', [], setError)); }, [setError]);
  const loadAuditLogs    = useCallback(async (page = 1, append = false) => {
    const data = await safeFetch<AuditLog[]>(`/api/audit/logs?page=${page}&pageSize=20`, [], setError);
    if (append) { setAuditLogs(prev => [...prev, ...data]); } else { setAuditLogs(data); }
    setAuditHasMore(data.length >= 20);
    setAuditPage(page);
  }, []);

  const loadAll = async () => {
    setLoading(true); setError(null);
    try {
      const [gmb, gsv, ssv, ulip, mort, loyal] = await Promise.all([
        api.get<GmbFactor[]>(scoped('/api/admin/factors/gmb')),
        api.get<GsvFactor[]>(scoped('/api/admin/factors/gsv')),
        api.get<SsvFactor[]>(scoped('/api/admin/factors/ssv')),
        api.get<UlipCharge[]>(scoped('/api/admin/factors/ulip-charges')),
        api.get<MortalityRate[]>(scoped('/api/admin/factors/mortality')),
        api.get<LoyaltyFactor[]>(scoped('/api/admin/factors/loyalty')),
      ]);
      setGmbRows(gmb.data);
      setGsvRows(gsv.data);
      setSsvRows(ssv.data);
      setUlipRows(ulip.data);
      setMortalityRows(mort.data);
      setLoyaltyRows(loyal.data);
    } catch (e: any) {
      const status = e?.response?.status;
      const msg = e?.response?.data?.error || e?.message || 'Unknown error';
      setError(`Could not load factor tables. ${status ? `HTTP ${status}: ` : ''}${msg}`);
    } finally { setLoading(false); }
    // Load new tabs in parallel, gracefully
    loadUsers(); loadRoles(); loadModules(); loadIntegrations(); loadAuditLogs();
  };

  useEffect(() => { loadAll(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    safeFetch<any[]>('/api/admin/products', [], setError).then(list => {
      setProducts(list.map(p => ({ code: p.code, name: p.name })));
    }).catch(() => setProducts([]));
  }, []);

  // --- update helpers ---
  const patchGmb = async (id: number, factor: number) => {
    await api.put(scoped(`/api/admin/factors/gmb/${id}`), { factor });
    setGmbRows(prev => prev.map(r => r.id === id ? { ...r, factor } : r));
  };
  const patchGsv = async (id: number, factorPercent: number) => {
    await api.put(scoped(`/api/admin/factors/gsv/${id}`), { factorPercent });
    setGsvRows(prev => prev.map(r => r.id === id ? { ...r, factorPercent } : r));
  };
  const patchSsv = async (id: number, f1: number, f2: number) => {
    await api.put(scoped(`/api/admin/factors/ssv/${id}`), { ssvFactor1Percent: f1, ssvFactor2Percent: f2 });
    setSsvRows(prev => prev.map(r => r.id === id ? { ...r, factor1: f1, factor2: f2 } : r));
  };
  const patchUlip = async (id: number, chargeValue: number) => {
    await api.put(scoped(`/api/admin/factors/ulip-charges/${id}`), { chargeValue });
    setUlipRows(prev => prev.map(r => r.id === id ? { ...r, chargeValue } : r));
  };
  const patchMortality = async (id: number, rate: number) => {
    await api.put(scoped(`/api/admin/factors/mortality/${id}`), { rate });
    setMortalityRows(prev => prev.map(r => r.id === id ? { ...r, rate } : r));
  };
  const patchLoyalty = async (id: number, ratePercent: number) => {
    await api.put(scoped(`/api/admin/factors/loyalty/${id}`), { ratePercent });
    setLoyaltyRows(prev => prev.map(r => r.id === id ? { ...r, ratePercent } : r));
  };

  return (
    <div className="space-y-6">
      {/* Page heading */}
      <div className="flex items-start justify-between">
        <div>
          <h2 className="text-2xl font-bold text-[#004282] flex items-center gap-2">
            <Settings size={22} />
            Admin Master — Formula &amp; Factor Tables &amp; System Management
            <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
          </h2>
          <p className="mt-2 text-slate-500 text-sm">
            View and edit calculation factors, manage users, roles, integrations, and audit logs.
            All changes take effect immediately.
          </p>
        </div>
        <button
          onClick={loadAll}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold
                     bg-slate-100 text-slate-600 rounded-lg hover:bg-slate-200 transition"
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

      {/* Product/Version scope placeholder */}
      <div className="flex flex-wrap gap-3 items-end bg-white border border-slate-200 rounded-xl p-4 shadow-sm">
        <div>
          <label className="block text-xs font-semibold text-slate-500 mb-1">Product Code (optional)</label>
          <select
            value={productScope}
            onChange={e => setProductScope(e.target.value)}
            className="border border-slate-200 rounded-lg px-3 py-2 text-sm min-w-[200px]"
          >
            <option value="">All products</option>
            {products.map(p => (
              <option key={p.code} value={p.code}>{p.name} ({p.code})</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-semibold text-slate-500 mb-1">Version (optional)</label>
          <input
            value={versionScope}
            onChange={e => setVersionScope(e.target.value)}
            className="border border-slate-200 rounded-lg px-3 py-2 text-sm"
            placeholder="e.g. v1"
          />
        </div>
        <div className="text-xs text-slate-500">
          Scope filters factor-table APIs by product/version (when supported). Blank = all.
        </div>
        <button className="ml-auto text-xs px-3 py-2 border rounded-lg text-slate-500 bg-slate-50" disabled>
          CSV/Excel upload (coming soon)
        </button>
      </div>

      {/* Tab bar */}
      <div className="flex flex-wrap gap-2">
        {TABS.map(t => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`px-4 py-2 rounded-full text-sm font-semibold transition
              ${tab === t.id
                ? 'bg-[#004282] text-white shadow-md'
                : 'bg-white text-[#004282] border border-[#004282] hover:bg-blue-50'}`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {loading && (
        <div className="text-center py-12 text-slate-400 text-sm">Loading factor tables…</div>
      )}

      {!loading && tab === 'formulas' && (
        <div className="grid md:grid-cols-2 gap-6">
          <FormulaCard title="Endowment Formulas" items={[
            'SAD = Max(10 × AP, GMB)',
            'GMB = AP × GMB_Factor(PPT, PT, Age, Option) × HP% × Channel%',
            'GSV = Max(0, Total Premiums Paid × GSV_Factor% − Cumulative SB)',
            'SSV = (F1/100 × PaidUpGMB) + (F2/100 × IncomeComponent)',
            'SV  = Max(GSV, SSV)',
            'DB  = Max(SAD, SV, 105% × TPP)',
            'Twin Income payout years: see Loyalty Factors table',
          ]} />
          <FormulaCard title="ULIP Formulas" items={[
            'PAC  = 0%  (full premium invested)',
            'FMC  = 1.35% p.a. of Fund Value',
            'PC   = ₹100/month × 12 = ₹1,200/yr (years 1–10 only)',
            'MC   = (SAR × MortalityRate) / 1,000',
            'SAR  = Max(SA − FV, 0)',
            'Net  = FV + Premium − MC − PC',
            'FV   = Net × (1 + growth%) × (1 − FMC%)',
            'LA   = FV × 0.1% (year 7 onwards)',
            'WB   = FV × 3% (at years 10, 15, 20, …)',
            'DB   = Max(SA, FV, 105% × Cumulative Premiums)',
          ]} />
          <FormulaCard title="Abbreviations" items={[
            'AP / ANP — Annual / Annualized Premium',
            'PPT — Premium Payment Term',
            'PT  — Policy Term',
            'SA  — Sum Assured',
            'SAD — Sum Assured on Death',
            'SAR — Sum At Risk',
            'NAV — Net Asset Value (ULIP fund value)',
            'TPP — Total Premiums Paid',
            'RPU — Reduced Paid-Up status flag',
            'GI  — Guaranteed Income (Endowment)',
            'LI  — Loyalty Income (Endowment)',
          ]} />
          <FormulaCard title="Twin Income Payout Years (Exact)" items={[
            'PPT 7  / PT 15 : years  5,  6, 10, 11',
            'PPT 10 / PT 20 : years  8,  9, 14, 15',
            'PPT 12 / PT 25 : years 10, 11, 15, 16, 20, 21',
            'PPT 15 / PT 25 : years 13, 14, 18, 19, 23, 24',
            'Payout = 105% × AP in each Twin Income year',
          ]} />
        </div>
      )}

      {!loading && tab === 'gmb'       && <GmbTable       rows={gmbRows}       onUpdate={patchGmb} />}
      {!loading && tab === 'gsv'       && <GsvTable       rows={gsvRows}       onUpdate={patchGsv} />}
      {!loading && tab === 'ssv'       && <SsvTable       rows={ssvRows}       onUpdate={patchSsv} />}
      {!loading && tab === 'ulip'      && <UlipChargesTable rows={ulipRows}    onUpdate={patchUlip} />}
      {!loading && tab === 'mortality' && <MortalityTable  rows={mortalityRows} onUpdate={patchMortality} />}
      {!loading && tab === 'loyalty'   && <LoyaltyTable    rows={loyaltyRows}  onUpdate={patchLoyalty} />}

      {/* New admin tabs — always rendered (not gated by factor-table loading) */}
      {tab === 'users'        && <UserManagementTab users={adminUsers} onReload={loadUsers} />}
      {tab === 'roles'        && <RoleManagementTab roles={adminRoles} onReload={loadRoles} />}
      {tab === 'modules'      && <ModuleAccessTab modules={modules} roles={adminRoles} onReload={() => { loadModules(); loadRoles(); }} />}
      {tab === 'integrations' && <IntegrationConfigTab configs={integrations} onReload={loadIntegrations} />}
      {tab === 'audit'        && <AuditLogsTab logs={auditLogs} hasMore={auditHasMore} onLoadMore={() => loadAuditLogs(auditPage + 1, true)} />}
    </div>
  );
}

/**
 * Configuration — Product-driven configuration editor.
 *
 * Replaces the configuration-related parts of AdminMaster.tsx with a
 * product + UIN–scoped workflow.  Sections are shown/hidden based on
 * the selected product type (Traditional vs ULIP).
 *
 * Capabilities:
 *   - Product + UIN selector
 *   - Collapsible factor-table editors (GMB, GSV, SSV, Mortality, Loyalty, ULIP Charges, Fund Config)
 *   - Formula editor with parameter insertion, validation, and version history
 *   - Integration config key-value editor
 *   - CSV import / export for every factor table
 */
import { useState, useEffect, useCallback, useRef } from 'react';
import {
  Settings, RefreshCw, AlertCircle, Edit3, Save, X, Info,
  Plus, ChevronDown, ChevronRight, Download, Upload,
  History, Check, FileText,
} from 'lucide-react';
import { apiClient } from '../utils/apiClient';

// ────────────────────────────────────────────────────────────────────────────
// Types
// ────────────────────────────────────────────────────────────────────────────
interface ProductItem    { code: string; name: string; productType: string; }
interface UinItem        { id: number; version: string; isActive: boolean; }

interface GmbFactor      { id: number; ppt: number; pt: number; entryAgeMin: number; entryAgeMax: number; option: string; factor: number; }
interface GsvFactor      { id: number; ppt: number; policyYear: number; factorPercent: number; }
interface SsvFactor      { id: number; ppt: number; policyYear: number; factor1: number; factor2: number; }
interface MortalityRate  { id: number; gender: string; age: number; rate: number; }
interface LoyaltyFactor  { id: number; ppt: number; policyYearFrom: number; policyYearTo?: number; ratePercent: number; }
interface UlipCharge     { id: number; productId: number; chargeType: string; chargeValue: number; chargeFrequency: string; }
interface FundConfig     { id: number; fundType: string; navBase: number; minAlloc: number; maxAlloc: number; }

interface FormulaParam   { name: string; description: string; dataType: string; }
interface FormulaVersion { version: number; changedBy: string; changedOn: string; expression: string; }
interface ValidationResult { isValid: boolean; errors: string[]; }

interface IntegrationConfig {
  id: number; name: string; baseUrl: string; authType: string;
  timeout: number; mockMode: boolean; isActive: boolean;
}

// ────────────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────────────
async function safeFetch<T>(url: string, fallback: T): Promise<T> {
  try { return (await apiClient.get<T>(url)).data; }
  catch { return fallback; }
}

function downloadCsv(filename: string, rows: Record<string, unknown>[]) {
  if (rows.length === 0) return;
  const keys = Object.keys(rows[0]);
  const header = keys.join(',');
  const body = rows.map(r => keys.map(k => String(r[k] ?? '')).join(',')).join('\n');
  const blob = new Blob([header + '\n' + body], { type: 'text/csv' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url; a.download = filename; a.click();
  URL.revokeObjectURL(url);
}

/** RFC 4180–aware CSV field splitter (handles quoted fields with commas). */
function splitCsvRow(line: string): string[] {
  const fields: string[] = [];
  let i = 0;
  while (i <= line.length) {
    if (i === line.length) { fields.push(''); break; }
    if (line[i] === '"') {
      let val = '';
      i++; // skip opening quote
      while (i < line.length) {
        if (line[i] === '"' && line[i + 1] === '"') { val += '"'; i += 2; }
        else if (line[i] === '"') { i++; break; }
        else { val += line[i]; i++; }
      }
      if (i < line.length && line[i] === ',') i++; // skip comma
      fields.push(val);
    } else {
      const next = line.indexOf(',', i);
      if (next === -1) { fields.push(line.slice(i)); break; }
      fields.push(line.slice(i, next));
      i = next + 1;
    }
  }
  return fields;
}

function parseCsv<T>(text: string): T[] {
  const lines = text.trim().split('\n').map(l => l.replace(/\r$/, ''));
  if (lines.length < 2) return [];
  const headers = splitCsvRow(lines[0]).map(h => h.trim());
  return lines.slice(1).map(line => {
    const vals = splitCsvRow(line).map(v => v.trim());
    const obj: Record<string, unknown> = {};
    headers.forEach((h, i) => {
      const raw = vals[i] ?? '';
      const num = Number(raw);
      obj[h] = raw !== '' && !isNaN(num) ? num : raw;
    });
    return obj as T;
  });
}

// ────────────────────────────────────────────────────────────────────────────
// Shared sub-components (same patterns as AdminMaster.tsx)
// ────────────────────────────────────────────────────────────────────────────
function EditableNumber({ value, onSave }: { value: number; onSave: (v: number) => void }) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft]     = useState(value);

  if (!editing)
    return (
      <span className="cursor-pointer text-[#004282] hover:underline flex items-center gap-1"
        title="Click to edit" onClick={() => { setDraft(value); setEditing(true); }}>
        {value}
        <Edit3 size={11} className="opacity-40" />
      </span>
    );

  return (
    <span className="flex items-center gap-1">
      <input type="number" step="any" value={draft}
        onChange={e => setDraft(parseFloat(e.target.value))}
        className="w-24 rounded border border-blue-300 px-1 py-0.5 text-xs focus:ring-1 focus:ring-[#007bff]"
        autoFocus />
      <button onClick={() => { onSave(draft); setEditing(false); }} title="Save"
        className="text-green-600 hover:text-green-800"><Save size={13} /></button>
      <button onClick={() => setEditing(false)} title="Cancel"
        className="text-red-500 hover:text-red-700"><X size={13} /></button>
    </span>
  );
}

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

// ────────────────────────────────────────────────────────────────────────────
// Collapsible Panel wrapper
// ────────────────────────────────────────────────────────────────────────────
function CollapsiblePanel({
  title, defaultOpen = false, children,
}: { title: string; defaultOpen?: boolean; children: React.ReactNode }) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <div className="border border-slate-200 rounded-xl overflow-hidden">
      <button onClick={() => setOpen(o => !o)}
        className="w-full flex items-center gap-2 px-5 py-3 bg-blue-50/40 hover:bg-blue-50/70 transition text-left">
        {open ? <ChevronDown size={16} className="text-[#004282]" /> : <ChevronRight size={16} className="text-[#004282]" />}
        <span className="text-sm font-bold text-[#004282]">{title}</span>
      </button>
      {open && <div className="p-4 space-y-4">{children}</div>}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// CSV import button helper
// ────────────────────────────────────────────────────────────────────────────
function CsvImportButton<T>({ onImport }: { onImport: (rows: T[]) => void }) {
  const ref = useRef<HTMLInputElement>(null);
  const handleFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = ev => {
      const text = ev.target?.result as string;
      onImport(parseCsv<T>(text));
    };
    reader.readAsText(file);
    if (ref.current) ref.current.value = '';
  };
  return (
    <>
      <input ref={ref} type="file" accept=".csv" className="hidden" onChange={handleFile} />
      <button onClick={() => ref.current?.click()}
        className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
        <Upload size={13} /> Import CSV
      </button>
    </>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Factor table panels
// ────────────────────────────────────────────────────────────────────────────
function GmbPanel({ rows, onUpdate, onExport, onImport }: {
  rows: GmbFactor[]; onUpdate: (id: number, factor: number) => void;
  onExport: () => void; onImport: (rows: GmbFactor[]) => void;
}) {
  return (
    <Section title="GMB Factors — Guaranteed Maturity Benefit"
      subtitle="Lookup by PPT × PT × Age Range × Option. Factor = GMB / Annual Premium.">
      <div className="flex gap-2 px-4 pt-3 pb-1 justify-end">
        <button onClick={onExport}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
          <Download size={13} /> Export CSV
        </button>
        <CsvImportButton<GmbFactor> onImport={onImport} />
      </div>
      {rows.length === 0 ? <EmptyState message="No GMB factors loaded." /> : (
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
      )}
    </Section>
  );
}

function GsvPanel({ rows, onUpdate, onExport, onImport }: {
  rows: GsvFactor[]; onUpdate: (id: number, pct: number) => void;
  onExport: () => void; onImport: (rows: GsvFactor[]) => void;
}) {
  const ppts = [...new Set(rows.map(r => r.ppt))].sort((a, b) => a - b);
  return (
    <Section title="GSV Factors — Guaranteed Surrender Value"
      subtitle="GSV = Total Premiums Paid × (Factor / 100). Grouped by PPT.">
      <div className="flex gap-2 px-4 pt-3 pb-1 justify-end">
        <button onClick={onExport}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
          <Download size={13} /> Export CSV
        </button>
        <CsvImportButton<GsvFactor> onImport={onImport} />
      </div>
      {rows.length === 0 ? <EmptyState message="No GSV factors loaded." /> : (
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
      )}
    </Section>
  );
}

function SsvPanel({ rows, onUpdate, onExport, onImport }: {
  rows: SsvFactor[]; onUpdate: (id: number, f1: number, f2: number) => void;
  onExport: () => void; onImport: (rows: SsvFactor[]) => void;
}) {
  const ppts = [...new Set(rows.map(r => r.ppt))].sort((a, b) => a - b);
  return (
    <Section title="SSV Factors — Special Surrender Value"
      subtitle="SSV = (F1/100 × PaidUpGMB) + (F2/100 × IncomeComponent)">
      <div className="flex gap-2 px-4 pt-3 pb-1 justify-end">
        <button onClick={onExport}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
          <Download size={13} /> Export CSV
        </button>
        <CsvImportButton<SsvFactor> onImport={onImport} />
      </div>
      {rows.length === 0 ? <EmptyState message="No SSV factors loaded." /> : (
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
      )}
    </Section>
  );
}

function MortalityPanel({ rows, onUpdate, onExport, onImport }: {
  rows: MortalityRate[]; onUpdate: (id: number, rate: number) => void;
  onExport: () => void; onImport: (rows: MortalityRate[]) => void;
}) {
  const [gender, setGender] = useState<'Male' | 'Female'>('Male');
  const filtered = rows.filter(r => r.gender === gender).sort((a, b) => a.age - b.age);
  return (
    <Section title="Mortality Rates — per ₹1,000 Sum At Risk (SAR)"
      subtitle="MC = (SAR × Rate) / 1000. Looked up by Gender × Age.">
      <div className="flex items-center gap-2 px-4 pt-3 pb-1">
        <div className="flex gap-2 flex-1">
          {(['Male', 'Female'] as const).map(g => (
            <button key={g} onClick={() => setGender(g)}
              className={`px-3 py-1 rounded-full text-xs font-semibold transition
                ${gender === g ? 'bg-[#004282] text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>
              {g}
            </button>
          ))}
        </div>
        <button onClick={onExport}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
          <Download size={13} /> Export CSV
        </button>
        <CsvImportButton<MortalityRate> onImport={onImport} />
      </div>
      {filtered.length === 0 ? <EmptyState message="No mortality rates loaded." /> : (
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
      )}
    </Section>
  );
}

function LoyaltyPanel({ rows, onUpdate, onExport, onImport }: {
  rows: LoyaltyFactor[]; onUpdate: (id: number, rate: number) => void;
  onExport: () => void; onImport: (rows: LoyaltyFactor[]) => void;
}) {
  const ppts = [...new Set(rows.map(r => r.ppt))].sort((a, b) => a - b);
  return (
    <Section title="Loyalty Income Factors"
      subtitle="LI = AP × (Rate / 100). Rate > 0 only in Loyalty Income payout years.">
      <div className="flex gap-2 px-4 pt-3 pb-1 justify-end">
        <button onClick={onExport}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
          <Download size={13} /> Export CSV
        </button>
        <CsvImportButton<LoyaltyFactor> onImport={onImport} />
      </div>
      {rows.length === 0 ? <EmptyState message="No loyalty factors loaded." /> : (
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
      )}
    </Section>
  );
}

function UlipChargesPanel({ rows, onUpdate, onExport, onImport }: {
  rows: UlipCharge[]; onUpdate: (id: number, val: number) => void;
  onExport: () => void; onImport: (rows: UlipCharge[]) => void;
}) {
  const descriptions: Record<string, string> = {
    PremiumAllocation: 'PAC — Premium Allocation Charge (% of AP). Product spec: 0%.',
    FMC: 'Fund Management Charge (% of FV p.a.). Product spec: 1.35%.',
    PolicyAdmin: 'Policy Administration Charge (₹/month). Applied for first 10 years. Product spec: ₹100/month.',
  };
  return (
    <Section title="ULIP Charges" subtitle="Charges applied in ULIP Benefit Illustration calculations.">
      <div className="flex gap-2 px-4 pt-3 pb-1 justify-end">
        <button onClick={onExport}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
          <Download size={13} /> Export CSV
        </button>
        <CsvImportButton<UlipCharge> onImport={onImport} />
      </div>
      {rows.length === 0 ? <EmptyState message="No ULIP charges loaded." /> : (
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
      )}
    </Section>
  );
}

function FundConfigPanel({ rows, onUpdate, onExport, onImport }: {
  rows: FundConfig[]; onUpdate: (id: number, field: string, val: number) => void;
  onExport: () => void; onImport: (rows: FundConfig[]) => void;
}) {
  return (
    <Section title="Fund Configuration" subtitle="ULIP fund types and allocation constraints.">
      <div className="flex gap-2 px-4 pt-3 pb-1 justify-end">
        <button onClick={onExport}
          className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition">
          <Download size={13} /> Export CSV
        </button>
        <CsvImportButton<FundConfig> onImport={onImport} />
      </div>
      {rows.length === 0 ? <EmptyState message="No fund configuration loaded." /> : (
        <table className="w-full text-xs">
          <THead cols={['Fund Type', 'NAV Base', 'Min Alloc (%)', 'Max Alloc (%)']} />
          <tbody className="divide-y divide-slate-100">
            {rows.map(r => (
              <tr key={r.id} className="hover:bg-blue-50/20 text-slate-700">
                <td className="px-3 py-2 font-semibold text-[#004282]">{r.fundType}</td>
                <td className="px-3 py-2 text-center">
                  <EditableNumber value={r.navBase} onSave={v => onUpdate(r.id, 'navBase', v)} />
                </td>
                <td className="px-3 py-2 text-center">
                  <EditableNumber value={r.minAlloc} onSave={v => onUpdate(r.id, 'minAlloc', v)} />
                </td>
                <td className="px-3 py-2 text-center">
                  <EditableNumber value={r.maxAlloc} onSave={v => onUpdate(r.id, 'maxAlloc', v)} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Section>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Integration Config panel
// ────────────────────────────────────────────────────────────────────────────
const EMPTY_INTEGRATION: Omit<IntegrationConfig, 'id'> = {
  name: '', baseUrl: '', authType: 'Bearer', timeout: 30, mockMode: false, isActive: true,
};

function IntegrationPanel({ configs, onReload }: { configs: IntegrationConfig[]; onReload: () => void }) {
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState(EMPTY_INTEGRATION);

  const startEdit = (c: IntegrationConfig) => {
    setForm({ name: c.name, baseUrl: c.baseUrl, authType: c.authType, timeout: c.timeout, mockMode: c.mockMode, isActive: c.isActive });
    setEditingId(c.id);
    setCreating(true);
  };

  const handleSave = async () => {
    try {
      if (editingId != null) {
        await apiClient.put(`/api/admin/integrations/${editingId}`, form);
      } else {
        await apiClient.post('/api/admin/integrations', form);
      }
      setCreating(false);
      setEditingId(null);
      setForm(EMPTY_INTEGRATION);
      onReload();
    } catch (e: unknown) {
      console.error('Integration save failed', e);
    }
  };

  const toggleMock = async (c: IntegrationConfig) => {
    try {
      await apiClient.put(`/api/admin/integrations/${c.id}`, { ...c, mockMode: !c.mockMode });
      onReload();
    } catch (e: unknown) {
      console.error('Toggle mock failed', e);
    }
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
          <input type="number" placeholder="Timeout (s)" value={form.timeout}
            onChange={e => setForm(p => ({ ...p, timeout: Number(e.target.value) }))}
            className="rounded border border-blue-300 px-2 py-1.5 text-xs focus:ring-1 focus:ring-[#007bff]" />
          <label className="flex items-center gap-2 text-xs">
            <input type="checkbox" checked={form.mockMode}
              onChange={e => setForm(p => ({ ...p, mockMode: e.target.checked }))} className="accent-[#004282]" />
            Mock Mode
          </label>
          <label className="flex items-center gap-2 text-xs">
            <input type="checkbox" checked={form.isActive}
              onChange={e => setForm(p => ({ ...p, isActive: e.target.checked }))} className="accent-[#004282]" />
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
                    ${c.mockMode ? 'bg-amber-100 text-amber-700' : 'bg-green-100 text-green-700'}`}>
                    {c.mockMode ? 'Mock' : 'Live'}
                  </span>
                </td>
                <td className="px-3 py-2 text-center">
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold
                    ${c.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                    {c.isActive ? 'Yes' : 'No'}
                  </span>
                </td>
                <td className="px-3 py-2 text-center flex items-center justify-center gap-2">
                  <button onClick={() => startEdit(c)} className="text-[#004282] hover:text-[#007bff]" title="Edit">
                    <Edit3 size={13} />
                  </button>
                  <button onClick={() => toggleMock(c)}
                    className={`text-xs font-semibold px-2 py-0.5 rounded ${c.mockMode
                      ? 'bg-green-100 text-green-700 hover:bg-green-200'
                      : 'bg-amber-100 text-amber-700 hover:bg-amber-200'}`}>
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

// ────────────────────────────────────────────────────────────────────────────
// Formula Editor
// ────────────────────────────────────────────────────────────────────────────
const OPERATORS = ['+', '-', '*', '/', '%', '(', ')', 'IF', 'AND', 'OR', '>', '<', '=', '>=', '<=', '<>', 'NESTED', 'LOOKUP'] as const;

function FormulaEditor({
  productCode, uin,
}: { productCode: string; uin: string }) {
  const [expression, setExpression]   = useState('');
  const [savedExpr, setSavedExpr]     = useState('');
  const [params, setParams]           = useState<FormulaParam[]>([]);
  const [history, setHistory]         = useState<FormulaVersion[]>([]);
  const [showHistory, setShowHistory] = useState(false);
  const [validation, setValidation]   = useState<ValidationResult | null>(null);
  const [validating, setValidating]   = useState(false);
  const [saving, setSaving]           = useState(false);
  const [saveMsg, setSaveMsg]         = useState<string | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Load parameters, current formula, and history
  useEffect(() => {
    if (!productCode || !uin) return;
    (async () => {
      const [p, formula, h] = await Promise.all([
        safeFetch<FormulaParam[]>(`/api/configuration/parameters?productCode=${productCode}&uin=${uin}`, []),
        safeFetch<{ expression: string }>(`/api/configuration/formula?productCode=${productCode}&uin=${uin}`, { expression: '' }),
        safeFetch<FormulaVersion[]>(`/api/configuration/formula-history?productCode=${productCode}&uin=${uin}`, []),
      ]);
      setParams(p);
      setExpression(formula.expression);
      setSavedExpr(formula.expression);
      setHistory(h);
      setValidation(null);
      setSaveMsg(null);
    })();
  }, [productCode, uin]);

  const insertAtCursor = (text: string) => {
    const ta = textareaRef.current;
    if (!ta) { setExpression(prev => prev + text); return; }
    const start = ta.selectionStart;
    const end = ta.selectionEnd;
    const before = expression.slice(0, start);
    const after = expression.slice(end);
    const next = before + text + after;
    setExpression(next);
    // Restore cursor position after React re-render
    requestAnimationFrame(() => {
      ta.focus();
      ta.selectionStart = ta.selectionEnd = start + text.length;
    });
  };

  const handleValidate = async () => {
    setValidating(true);
    setSaveMsg(null);
    try {
      const res = await apiClient.post<ValidationResult>('/api/configuration/formula/validate', {
        formulaExpression: expression,
        parameterNames: params.map(p => p.name),
      });
      setValidation(res.data);
    } catch {
      setValidation({ isValid: false, errors: ['Validation request failed. Please try again.'] });
    } finally {
      setValidating(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    setSaveMsg(null);
    try {
      await apiClient.post('/api/configuration/formula', {
        productCode,
        uin,
        formulaExpression: expression,
      });
      setSavedExpr(expression);
      setSaveMsg('Formula saved successfully.');
      // Refresh history
      const h = await safeFetch<FormulaVersion[]>(
        `/api/configuration/formula-history?productCode=${productCode}&uin=${uin}`, [],
      );
      setHistory(h);
    } catch {
      setSaveMsg('Failed to save formula.');
    } finally {
      setSaving(false);
    }
  };

  const handleRestore = (ver: FormulaVersion) => {
    setExpression(ver.expression);
    setValidation(null);
    setSaveMsg(null);
    setShowHistory(false);
  };

  const isDirty = expression !== savedExpr;

  return (
    <div className="space-y-4">
      {/* Formula textarea */}
      <div>
        <label className="text-xs font-semibold text-[#004282] mb-1 flex items-center gap-1">
          <FileText size={13} /> Formula Expression
        </label>
        <textarea ref={textareaRef} value={expression}
          onChange={e => { setExpression(e.target.value); setValidation(null); setSaveMsg(null); }}
          rows={6} spellCheck={false}
          className="w-full font-mono text-xs rounded-lg border border-blue-300 px-3 py-2 focus:ring-2 focus:ring-[#007bff] focus:border-transparent resize-y"
          placeholder="Enter formula expression…" />
      </div>

      {/* Operator buttons */}
      <div>
        <p className="text-xs font-semibold text-slate-500 mb-1">Operators</p>
        <div className="flex flex-wrap gap-1">
          {OPERATORS.map(op => (
            <button key={op} onClick={() => insertAtCursor(` ${op} `)}
              className="px-2 py-1 text-xs font-mono bg-slate-100 text-slate-700 rounded hover:bg-slate-200 transition">
              {op}
            </button>
          ))}
        </div>
      </div>

      {/* Available parameters */}
      <div>
        <p className="text-xs font-semibold text-slate-500 mb-1">Available Parameters</p>
        {params.length === 0 ? (
          <p className="text-xs text-slate-400 italic">No parameters available for this product/UIN.</p>
        ) : (
          <div className="flex flex-wrap gap-1.5 max-h-40 overflow-y-auto">
            {params.map(p => (
              <button key={p.name} onClick={() => insertAtCursor(`{${p.name}}`)}
                title={`${p.description || p.name} (${p.dataType})`}
                className="px-2.5 py-1 text-xs bg-blue-50 text-[#004282] border border-blue-200 rounded-full hover:bg-blue-100 transition font-semibold">
                {'{' + p.name + '}'}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Action buttons */}
      <div className="flex items-center gap-2 flex-wrap">
        <button onClick={handleValidate} disabled={validating || !expression.trim()}
          className="flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition disabled:opacity-50">
          {validating ? <RefreshCw size={13} className="animate-spin" /> : <Check size={13} />}
          Validate Formula
        </button>
        <button onClick={handleSave} disabled={saving || !expression.trim()}
          className="flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-green-600 text-white rounded-lg hover:bg-green-700 transition disabled:opacity-50">
          {saving ? <RefreshCw size={13} className="animate-spin" /> : <Save size={13} />}
          Save
        </button>
        <button onClick={() => { setExpression(savedExpr); setValidation(null); setSaveMsg(null); }}
          disabled={!isDirty}
          className="flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-slate-100 text-slate-600 rounded-lg hover:bg-slate-200 transition disabled:opacity-50">
          <X size={13} /> Cancel
        </button>
        <button onClick={() => setShowHistory(h => !h)}
          className="flex items-center gap-1 px-4 py-1.5 text-xs font-semibold bg-slate-100 text-slate-600 rounded-lg hover:bg-slate-200 transition ml-auto">
          <History size={13} /> {showHistory ? 'Hide History' : 'Version History'}
        </button>
      </div>

      {/* Validation result */}
      {validation && (
        <div className={`rounded-lg p-3 text-xs ${validation.isValid
          ? 'bg-green-50 border border-green-200 text-green-700'
          : 'bg-red-50 border border-red-200 text-red-700'}`}>
          {validation.isValid ? (
            <span className="flex items-center gap-1"><Check size={13} /> Formula is valid.</span>
          ) : (
            <div>
              <span className="flex items-center gap-1 font-semibold mb-1"><AlertCircle size={13} /> Validation errors:</span>
              <ul className="list-disc ml-5 space-y-0.5">
                {validation.errors.map((err, i) => <li key={i}>{err}</li>)}
              </ul>
            </div>
          )}
        </div>
      )}

      {/* Save message */}
      {saveMsg && (
        <div className={`rounded-lg p-3 text-xs ${saveMsg.includes('success')
          ? 'bg-green-50 border border-green-200 text-green-700'
          : 'bg-red-50 border border-red-200 text-red-700'}`}>
          {saveMsg}
        </div>
      )}

      {/* Formula preview */}
      {expression.trim() && (
        <div>
          <p className="text-xs font-semibold text-slate-500 mb-1">Preview</p>
          <div className="font-mono text-xs bg-slate-50 rounded-lg p-3 border border-slate-200 whitespace-pre-wrap break-all text-slate-700">
            {expression}
          </div>
        </div>
      )}

      {/* Version history */}
      {showHistory && (
        <div className="border border-slate-200 rounded-lg overflow-hidden">
          <div className="px-4 py-2 bg-slate-50 border-b border-slate-200">
            <span className="text-xs font-bold text-[#004282] flex items-center gap-1">
              <History size={13} /> Version History
            </span>
          </div>
          {history.length === 0 ? (
            <div className="p-4 text-xs text-slate-400 text-center">No version history available.</div>
          ) : (
            <table className="w-full text-xs">
              <THead cols={['Version', 'Changed By', 'Changed On', 'Formula', 'Action']} />
              <tbody className="divide-y divide-slate-100">
                {history.map(v => (
                  <tr key={v.version} className="hover:bg-blue-50/20 text-slate-700">
                    <td className="px-3 py-2 text-center font-semibold text-[#004282]">v{v.version}</td>
                    <td className="px-3 py-2 text-center">{v.changedBy}</td>
                    <td className="px-3 py-2 text-center">{new Date(v.changedOn).toLocaleString()}</td>
                    <td className="px-3 py-2 font-mono max-w-xs truncate" title={v.expression}>{v.expression}</td>
                    <td className="px-3 py-2 text-center">
                      <button onClick={() => handleRestore(v)}
                        className="flex items-center gap-1 px-2 py-0.5 text-xs font-semibold bg-[#004282] text-white rounded hover:bg-[#003060] transition">
                        <RefreshCw size={11} /> Restore
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Section definitions per product type
// ────────────────────────────────────────────────────────────────────────────
type SectionId = 'gmb' | 'gsv' | 'ssv' | 'mortality' | 'loyalty' | 'ulipCharges' | 'fundConfig' | 'integrations' | 'formulas';

const TRADITIONAL_SECTIONS: SectionId[] = ['gmb', 'gsv', 'ssv', 'mortality', 'loyalty', 'integrations', 'formulas'];
const ULIP_SECTIONS: SectionId[]        = ['gmb', 'gsv', 'ssv', 'mortality', 'loyalty', 'ulipCharges', 'fundConfig', 'integrations', 'formulas'];

const SECTION_LABELS: Record<SectionId, string> = {
  gmb:          'GMB Factors',
  gsv:          'GSV Factors',
  ssv:          'SSV Factors',
  mortality:    'Mortality Charges',
  loyalty:      'Loyalty Factors',
  ulipCharges:  'ULIP Charges',
  fundConfig:   'Fund Configuration',
  integrations: 'Integration Config',
  formulas:     'Calculation Formulas',
};

// ────────────────────────────────────────────────────────────────────────────
// Main component
// ────────────────────────────────────────────────────────────────────────────
export default function Configuration() {
  // Product / UIN selectors
  const [products, setProducts]         = useState<ProductItem[]>([]);
  const [selectedProduct, setSelectedProduct] = useState('');
  const [uins, setUins]                 = useState<UinItem[]>([]);
  const [selectedUin, setSelectedUin]   = useState('');

  // Factor data
  const [gmbRows, setGmbRows]           = useState<GmbFactor[]>([]);
  const [gsvRows, setGsvRows]           = useState<GsvFactor[]>([]);
  const [ssvRows, setSsvRows]           = useState<SsvFactor[]>([]);
  const [mortalityRows, setMortalityRows] = useState<MortalityRate[]>([]);
  const [loyaltyRows, setLoyaltyRows]   = useState<LoyaltyFactor[]>([]);
  const [ulipRows, setUlipRows]         = useState<UlipCharge[]>([]);
  const [fundRows, setFundRows]         = useState<FundConfig[]>([]);
  const [integrations, setIntegrations] = useState<IntegrationConfig[]>([]);

  const [loading, setLoading]   = useState(false);
  const [error, setError]       = useState<string | null>(null);

  const productType = products.find(p => p.code === selectedProduct)?.productType ?? '';
  const applicableSections: SectionId[] = productType === 'ULIP' ? ULIP_SECTIONS : TRADITIONAL_SECTIONS;

  // ── Load products on mount ──
  useEffect(() => {
    (async () => {
      const prods = await safeFetch<ProductItem[]>('/api/configuration/products', []);
      setProducts(prods);
    })();
  }, []);

  // ── Load UINs when product changes ──
  useEffect(() => {
    if (!selectedProduct) { setUins([]); setSelectedUin(''); return; }
    (async () => {
      const u = await safeFetch<UinItem[]>(`/api/configuration/uins?productCode=${selectedProduct}`, []);
      setUins(u);
      setSelectedUin('');
    })();
  }, [selectedProduct]);

  // ── Scoped URL builder ──
  const scoped = useCallback((path: string) => {
    const params = new URLSearchParams();
    if (selectedProduct) params.append('productCode', selectedProduct);
    if (selectedUin) params.append('version', selectedUin);
    const qs = params.toString();
    return qs ? `${path}?${qs}` : path;
  }, [selectedProduct, selectedUin]);

  // ── Load all factor data when product+UIN are selected ──
  const loadAll = useCallback(async () => {
    if (!selectedProduct || !selectedUin) return;
    setLoading(true);
    setError(null);
    try {
      const [gmb, gsv, ssv, mort, loyal, ulip, fund, integ] = await Promise.all([
        safeFetch<GmbFactor[]>(scoped('/api/configuration/factors/gmb'), []),
        safeFetch<GsvFactor[]>(scoped('/api/configuration/factors/gsv'), []),
        safeFetch<SsvFactor[]>(scoped('/api/configuration/factors/ssv'), []),
        safeFetch<MortalityRate[]>(scoped('/api/configuration/factors/mortality'), []),
        safeFetch<LoyaltyFactor[]>(scoped('/api/configuration/factors/loyalty'), []),
        safeFetch<UlipCharge[]>(scoped('/api/configuration/factors/ulip-charges'), []),
        safeFetch<FundConfig[]>(scoped('/api/configuration/factors/fund-config'), []),
        safeFetch<IntegrationConfig[]>('/api/admin/integrations', []),
      ]);
      setGmbRows(gmb);
      setGsvRows(gsv);
      setSsvRows(ssv);
      setMortalityRows(mort);
      setLoyaltyRows(loyal);
      setUlipRows(ulip);
      setFundRows(fund);
      setIntegrations(integ);
    } catch (e: unknown) {
      const err = e as { response?: { status?: number; data?: { error?: string } }; message?: string };
      const status = err?.response?.status;
      const msg = err?.response?.data?.error || err?.message || 'Unknown error';
      setError(`Could not load configuration data. ${status ? `HTTP ${status}: ` : ''}${msg}`);
    } finally {
      setLoading(false);
    }
  }, [selectedProduct, selectedUin, scoped]);

  useEffect(() => { loadAll(); }, [loadAll]);

  // ── Inline-update helpers (use existing admin endpoints) ──
  const patchGmb = async (id: number, factor: number) => {
    await apiClient.put(scoped(`/api/admin/factors/gmb/${id}`), { factor });
    setGmbRows(prev => prev.map(r => r.id === id ? { ...r, factor } : r));
  };
  const patchGsv = async (id: number, factorPercent: number) => {
    await apiClient.put(scoped(`/api/admin/factors/gsv/${id}`), { factorPercent });
    setGsvRows(prev => prev.map(r => r.id === id ? { ...r, factorPercent } : r));
  };
  const patchSsv = async (id: number, factor1: number, factor2: number) => {
    await apiClient.put(scoped(`/api/admin/factors/ssv/${id}`), { factor1, factor2 });
    setSsvRows(prev => prev.map(r => r.id === id ? { ...r, factor1, factor2 } : r));
  };
  const patchMortality = async (id: number, rate: number) => {
    await apiClient.put(scoped(`/api/admin/factors/mortality/${id}`), { rate });
    setMortalityRows(prev => prev.map(r => r.id === id ? { ...r, rate } : r));
  };
  const patchLoyalty = async (id: number, ratePercent: number) => {
    await apiClient.put(scoped(`/api/admin/factors/loyalty/${id}`), { ratePercent });
    setLoyaltyRows(prev => prev.map(r => r.id === id ? { ...r, ratePercent } : r));
  };
  const patchUlip = async (id: number, chargeValue: number) => {
    await apiClient.put(scoped(`/api/admin/factors/ulip-charges/${id}`), { chargeValue });
    setUlipRows(prev => prev.map(r => r.id === id ? { ...r, chargeValue } : r));
  };
  const patchFund = async (id: number, field: string, val: number) => {
    await apiClient.put(scoped(`/api/admin/factors/fund-config/${id}`), { [field]: val });
    setFundRows(prev => prev.map(r => r.id === id ? { ...r, [field]: val } : r));
  };

  // ── Render section content ──
  const renderSection = (sectionId: SectionId) => {
    switch (sectionId) {
      case 'gmb':
        return (
          <GmbPanel rows={gmbRows} onUpdate={patchGmb}
            onExport={() => downloadCsv('gmb-factors.csv', gmbRows)}
            onImport={rows => setGmbRows(rows)} />
        );
      case 'gsv':
        return (
          <GsvPanel rows={gsvRows} onUpdate={patchGsv}
            onExport={() => downloadCsv('gsv-factors.csv', gsvRows)}
            onImport={rows => setGsvRows(rows)} />
        );
      case 'ssv':
        return (
          <SsvPanel rows={ssvRows} onUpdate={patchSsv}
            onExport={() => downloadCsv('ssv-factors.csv', ssvRows)}
            onImport={rows => setSsvRows(rows)} />
        );
      case 'mortality':
        return (
          <MortalityPanel rows={mortalityRows} onUpdate={patchMortality}
            onExport={() => downloadCsv('mortality-rates.csv', mortalityRows)}
            onImport={rows => setMortalityRows(rows)} />
        );
      case 'loyalty':
        return (
          <LoyaltyPanel rows={loyaltyRows} onUpdate={patchLoyalty}
            onExport={() => downloadCsv('loyalty-factors.csv', loyaltyRows)}
            onImport={rows => setLoyaltyRows(rows)} />
        );
      case 'ulipCharges':
        return (
          <UlipChargesPanel rows={ulipRows} onUpdate={patchUlip}
            onExport={() => downloadCsv('ulip-charges.csv', ulipRows)}
            onImport={rows => setUlipRows(rows)} />
        );
      case 'fundConfig':
        return (
          <FundConfigPanel rows={fundRows} onUpdate={patchFund}
            onExport={() => downloadCsv('fund-config.csv', fundRows)}
            onImport={rows => setFundRows(rows)} />
        );
      case 'integrations':
        return <IntegrationPanel configs={integrations} onReload={loadAll} />;
      case 'formulas':
        return (
          <Section title="Calculation Formulas" subtitle="Edit and validate calculation formulas for this product.">
            <div className="p-4">
              <FormulaEditor productCode={selectedProduct} uin={selectedUin} />
            </div>
          </Section>
        );
    }
  };

  // ────────────────────────────────────────────────────────────────────────
  // Render
  // ────────────────────────────────────────────────────────────────────────
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50/30 p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-[#004282] flex items-center justify-center shadow-lg">
            <Settings size={20} className="text-white" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-[#004282]">Configuration</h1>
            <p className="text-xs text-slate-500">Product-scoped factor tables, formulas &amp; integrations</p>
          </div>
        </div>
        {selectedProduct && selectedUin && (
          <button onClick={loadAll} disabled={loading}
            className="flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003060] transition disabled:opacity-50">
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} /> Refresh
          </button>
        )}
      </div>

      {/* Product + UIN Selector */}
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {/* Product dropdown */}
          <div>
            <label className="text-xs font-semibold text-[#004282] mb-1 block">Product</label>
            <select value={selectedProduct}
              onChange={e => setSelectedProduct(e.target.value)}
              className="w-full rounded-lg border border-blue-300 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff] focus:border-transparent">
              <option value="">— Select product —</option>
              {products.map(p => (
                <option key={p.code} value={p.code}>{p.name} ({p.code})</option>
              ))}
            </select>
            {selectedProduct && (
              <p className="text-xs text-slate-500 mt-1">
                Type: <span className={`font-semibold ${productType === 'ULIP' ? 'text-purple-700' : 'text-green-700'}`}>
                  {productType || 'Unknown'}
                </span>
              </p>
            )}
          </div>

          {/* UIN dropdown */}
          <div>
            <label className="text-xs font-semibold text-[#004282] mb-1 block">UIN / Version</label>
            <select value={selectedUin}
              onChange={e => setSelectedUin(e.target.value)}
              disabled={!selectedProduct}
              className="w-full rounded-lg border border-blue-300 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff] focus:border-transparent disabled:opacity-50">
              <option value="">— Select UIN —</option>
              {uins.map(u => (
                <option key={u.id} value={u.version}>
                  {u.version} {u.isActive ? '(Active)' : ''}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Error banner */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-4 flex items-start gap-3">
          <AlertCircle size={18} className="text-red-500 mt-0.5 shrink-0" />
          <div>
            <p className="text-sm font-semibold text-red-700">Error loading data</p>
            <p className="text-xs text-red-600 mt-0.5">{error}</p>
          </div>
        </div>
      )}

      {/* Loading */}
      {loading && (
        <div className="flex items-center justify-center gap-2 py-12 text-[#004282]">
          <RefreshCw size={20} className="animate-spin" />
          <span className="text-sm font-semibold">Loading configuration…</span>
        </div>
      )}

      {/* Prompt to select */}
      {!loading && (!selectedProduct || !selectedUin) && (
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-12 flex flex-col items-center gap-3">
          <Info size={28} className="text-slate-300" />
          <p className="text-sm text-slate-400 font-semibold">Select a product and UIN above to view configuration.</p>
        </div>
      )}

      {/* Collapsible sections */}
      {!loading && selectedProduct && selectedUin && (
        <div className="space-y-3">
          {applicableSections.map(sid => (
            <CollapsiblePanel key={sid} title={SECTION_LABELS[sid]}>
              {renderSection(sid)}
            </CollapsiblePanel>
          ))}
        </div>
      )}
    </div>
  );
}

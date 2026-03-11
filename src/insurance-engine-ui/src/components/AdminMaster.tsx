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
import { useState, useEffect } from 'react';
import { Settings, RefreshCw, AlertCircle, Edit3, Save, X, Info } from 'lucide-react';
import axios from 'axios';

const API_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5000';
const api = axios.create({ baseURL: API_URL });
api.interceptors.request.use(cfg => {
  const token = localStorage.getItem('auth_token');
  if (token && cfg.headers) cfg.headers.Authorization = `Bearer ${token}`;
  return cfg;
});

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------
interface GmbFactor  { id: number; ppt: number; pt: number; entryAgeMin: number; entryAgeMax: number; option: string; factor: number; }
interface GsvFactor  { id: number; ppt: number; policyYear: number; factorPercent: number; }
interface SsvFactor  { id: number; ppt: number; policyYear: number; factor1: number; factor2: number; }
interface UlipCharge { id: number; productId: number; chargeType: string; chargeValue: number; chargeFrequency: string; }
interface MortalityRate { id: number; gender: string; age: number; rate: number; }
interface LoyaltyFactor { id: number; ppt: number; policyYearFrom: number; policyYearTo?: number; ratePercent: number; }

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
  { id: 'formulas',   label: 'Formula Reference' },
  { id: 'gmb',        label: 'GMB Factors' },
  { id: 'gsv',        label: 'GSV Factors' },
  { id: 'ssv',        label: 'SSV Factors' },
  { id: 'ulip',       label: 'ULIP Charges' },
  { id: 'mortality',  label: 'Mortality Rates' },
  { id: 'loyalty',    label: 'Loyalty Factors' },
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

  const loadAll = async () => {
    setLoading(true); setError(null);
    try {
      const [gmb, gsv, ssv, ulip, mort, loyal] = await Promise.all([
        api.get<GmbFactor[]>('/api/admin/factors/gmb'),
        api.get<GsvFactor[]>('/api/admin/factors/gsv'),
        api.get<SsvFactor[]>('/api/admin/factors/ssv'),
        api.get<UlipCharge[]>('/api/admin/factors/ulip-charges'),
        api.get<MortalityRate[]>('/api/admin/factors/mortality'),
        api.get<LoyaltyFactor[]>('/api/admin/factors/loyalty'),
      ]);
      setGmbRows(gmb.data);
      setGsvRows(gsv.data);
      setSsvRows(ssv.data);
      setUlipRows(ulip.data);
      setMortalityRows(mort.data);
      setLoyaltyRows(loyal.data);
    } catch {
      setError('Could not load factor tables from server. Check that the API is running and you are logged in.');
    } finally { setLoading(false); }
  };

  useEffect(() => { loadAll(); }, []);

  // --- update helpers ---
  const patchGmb = async (id: number, factor: number) => {
    await api.put(`/api/admin/factors/gmb/${id}`, { factor });
    setGmbRows(prev => prev.map(r => r.id === id ? { ...r, factor } : r));
  };
  const patchGsv = async (id: number, factorPercent: number) => {
    await api.put(`/api/admin/factors/gsv/${id}`, { factorPercent });
    setGsvRows(prev => prev.map(r => r.id === id ? { ...r, factorPercent } : r));
  };
  const patchSsv = async (id: number, f1: number, f2: number) => {
    await api.put(`/api/admin/factors/ssv/${id}`, { ssvFactor1Percent: f1, ssvFactor2Percent: f2 });
    setSsvRows(prev => prev.map(r => r.id === id ? { ...r, factor1: f1, factor2: f2 } : r));
  };
  const patchUlip = async (id: number, chargeValue: number) => {
    await api.put(`/api/admin/factors/ulip-charges/${id}`, { chargeValue });
    setUlipRows(prev => prev.map(r => r.id === id ? { ...r, chargeValue } : r));
  };
  const patchMortality = async (id: number, rate: number) => {
    await api.put(`/api/admin/factors/mortality/${id}`, { rate });
    setMortalityRows(prev => prev.map(r => r.id === id ? { ...r, rate } : r));
  };
  const patchLoyalty = async (id: number, ratePercent: number) => {
    await api.put(`/api/admin/factors/loyalty/${id}`, { ratePercent });
    setLoyaltyRows(prev => prev.map(r => r.id === id ? { ...r, ratePercent } : r));
  };

  return (
    <div className="space-y-6">
      {/* Page heading */}
      <div className="flex items-start justify-between">
        <div>
          <h2 className="text-2xl font-bold text-[#004282] flex items-center gap-2">
            <Settings size={22} />
            Admin Master — Formula &amp; Factor Tables
            <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
          </h2>
          <p className="mt-2 text-slate-500 text-sm">
            View and edit calculation factors used in Endowment and ULIP illustrations.
            All changes are table-driven and take effect immediately.
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
    </div>
  );
}

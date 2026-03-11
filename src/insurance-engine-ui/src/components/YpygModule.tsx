import { useState } from 'react';
import { Search, BarChart3, AlertCircle } from 'lucide-react';
import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://ezytek1706-003-site3.rtempurl.com';
const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 2 });

// ─── Types ──────────────────────────────────────────────────────────────────

interface PolicyData {
  policyNumber: string;
  customerName: string;
  productType: string;
  productCode: string;
  annualPremium: number;
  policyTerm: number;
  premiumPayingTerm: number;
  premiumsPaid: number;
  sumAssured: number;
  fundValue: number;
  policyStatus: string;
  option: string;
  channel: string;
  entryAge: number;
}

interface YpygResult {
  policyNumber: string;
  productCode: string;
  annualPremium: number;
  policyTerm: number;
  premiumPayingTerm: number;
  maturityValue: number;
  surrenderValue: number;
  deathBenefit: number;
  sumAssuredOnDeath: number;
  maxLoanAmount: number;
  yearlyTable: {
    policyYear: number;
    annualPremium: number;
    totalPremiumsPaid: number;
    guaranteedIncome: number;
    loyaltyIncome: number;
    totalIncome: number;
    surrenderValue: number;
    deathBenefit: number;
    maturityBenefit: number;
  }[];
}

// ─── Sub-page: Policy Number mode ───────────────────────────────────────────

function PolicyNumberMode() {
  const [policyNumber, setPolicyNumber] = useState('');
  const [policy, setPolicy] = useState<PolicyData | null>(null);
  const [result, setResult] = useState<YpygResult | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [calcError, setCalcError] = useState<string | null>(null);
  const [lookupLoading, setLookupLoading] = useState(false);
  const [calcLoading, setCalcLoading] = useState(false);

  const handleSearch = async () => {
    if (!policyNumber.trim()) return;
    setLookupLoading(true);
    setLookupError(null);
    setPolicy(null);
    setResult(null);
    try {
      const res = await axios.get(`${API_URL}/api/ypyg/policy/${encodeURIComponent(policyNumber.trim())}`);
      setPolicy(res.data);
    } catch (e: any) {
      setLookupError(e?.response?.data?.error || 'Policy not found.');
    } finally {
      setLookupLoading(false);
    }
  };

  const handleCalculate = async () => {
    if (!policy) return;
    setCalcLoading(true);
    setCalcError(null);
    setResult(null);
    try {
      const res = await axios.post(`${API_URL}/api/ypyg/calculate`, {
        policyNumber: policy.policyNumber,
        productCode: policy.productCode,
        annualPremium: policy.annualPremium,
        policyTerm: policy.policyTerm,
        premiumPayingTerm: policy.premiumPayingTerm,
        premiumsPaid: policy.premiumsPaid,
        sumAssured: policy.sumAssured,
        entryAge: policy.entryAge,
        option: policy.option,
        channel: policy.channel,
        fundValue: policy.fundValue,
        surrenderFactor: 0.8,
      });
      setResult(res.data);
    } catch (e: any) {
      setCalcError(e?.response?.data?.error || 'Calculation failed.');
    } finally {
      setCalcLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Search bar */}
      <div className="flex gap-3">
        <input
          type="text"
          value={policyNumber}
          onChange={e => setPolicyNumber(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleSearch()}
          placeholder="Enter Policy Number (e.g. POL-001234)"
          className="flex-1 px-4 py-2.5 rounded-xl border border-slate-200 text-sm
                     focus:outline-none focus:ring-2 focus:ring-[#004282]"
        />
        <button
          onClick={handleSearch}
          disabled={lookupLoading}
          className="flex items-center gap-2 px-5 py-2.5 bg-[#004282] text-white rounded-xl
                     text-sm font-semibold hover:bg-[#003370] disabled:opacity-60 transition"
        >
          <Search size={15} />
          {lookupLoading ? 'Searching…' : 'Search'}
        </button>
      </div>

      {lookupError && (
        <ErrorBanner message={lookupError} />
      )}

      {policy && (
        <>
          {/* Policy details card */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-bold text-[#004282]">
                Policy Details
                <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
              </h3>
              <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                policy.policyStatus === 'In-Force'
                  ? 'bg-green-100 text-green-700'
                  : 'bg-amber-100 text-amber-700'
              }`}>
                {policy.policyStatus}
              </span>
            </div>
            <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
              {[
                ['Policy Number', policy.policyNumber],
                ['Customer Name', policy.customerName],
                ['Product', `${policy.productType} (${policy.productCode})`],
                ['Annual Premium', `₹ ${INR(policy.annualPremium)}`],
                ['Policy Term', `${policy.policyTerm} yrs`],
                ['Premium Paying Term', `${policy.premiumPayingTerm} yrs`],
                ['Premiums Paid', `${policy.premiumsPaid} yrs`],
                ['Sum Assured', `₹ ${INR(policy.sumAssured)}`],
                ['Entry Age', `${policy.entryAge} yrs`],
                ['Option', policy.option],
                ['Channel', policy.channel],
                ['Fund Value', `₹ ${INR(policy.fundValue)}`],
              ].map(([k, v]) => (
                <div key={k as string}>
                  <p className="text-xs text-slate-400 uppercase tracking-wider">{k}</p>
                  <p className="font-semibold text-slate-700 mt-0.5">{v}</p>
                </div>
              ))}
            </div>
            <button
              onClick={handleCalculate}
              disabled={calcLoading}
              className="mt-5 flex items-center gap-2 px-5 py-2.5 bg-[#004282] text-white rounded-xl
                         text-sm font-semibold hover:bg-[#003370] disabled:opacity-60 transition"
            >
              <BarChart3 size={15} />
              {calcLoading ? 'Calculating…' : 'Calculate Benefits'}
            </button>
          </div>

          {calcError && <ErrorBanner message={calcError} />}
          {result && <ResultSection result={result} />}
        </>
      )}
    </div>
  );
}

// ─── Sub-page: Input Value mode ──────────────────────────────────────────────

const DEFAULT_INPUTS = {
  policyNumber: '',
  annualPremium: 50000,
  policyTerm: 20,
  premiumPayingTerm: 10,
  premiumsPaid: 5,
  sumAssured: 500000,
  entryAge: 35,
  option: 'Immediate',
  channel: 'Other',
  fundValue: 0,
  surrenderFactor: 0.8,
};

function InputValueMode() {
  const [form, setForm] = useState(DEFAULT_INPUTS);
  const [result, setResult] = useState<YpygResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const set = (k: keyof typeof DEFAULT_INPUTS, v: string | number) =>
    setForm(f => ({ ...f, [k]: v }));

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const res = await axios.post(`${API_URL}/api/ypyg/calculate`, {
        ...form,
        productCode: 'CENTURY_INCOME',
      });
      setResult(res.data);
    } catch (e: any) {
      setError(e?.response?.data?.error || 'Calculation failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="grid lg:grid-cols-3 gap-8">
      {/* Input form */}
      <div className="lg:col-span-1">
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">Input Parameters</h3>

          <Field label="Policy Number (optional)">
            <input type="text" value={form.policyNumber}
              onChange={e => set('policyNumber', e.target.value)}
              className={INPUT_CLS} placeholder="Optional" />
          </Field>
          <Field label="Annual Premium (₹)">
            <input type="number" value={form.annualPremium}
              onChange={e => set('annualPremium', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Policy Term (yrs)">
            <input type="number" value={form.policyTerm}
              onChange={e => set('policyTerm', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Premium Paying Term (yrs)">
            <input type="number" value={form.premiumPayingTerm}
              onChange={e => set('premiumPayingTerm', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Premiums Paid">
            <input type="number" value={form.premiumsPaid}
              onChange={e => set('premiumsPaid', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Sum Assured (₹)">
            <input type="number" value={form.sumAssured}
              onChange={e => set('sumAssured', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Entry Age (yrs)">
            <input type="number" value={form.entryAge}
              onChange={e => set('entryAge', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Fund Value (₹)">
            <input type="number" value={form.fundValue}
              onChange={e => set('fundValue', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Surrender Factor">
            <input type="number" step="0.01" value={form.surrenderFactor}
              onChange={e => set('surrenderFactor', +e.target.value)}
              className={INPUT_CLS} />
          </Field>
          <Field label="Option">
            <select value={form.option}
              onChange={e => set('option', e.target.value)}
              className={INPUT_CLS}>
              <option>Immediate</option>
              <option>Deferred</option>
              <option>Twin</option>
            </select>
          </Field>
          <Field label="Channel">
            <select value={form.channel}
              onChange={e => set('channel', e.target.value)}
              className={INPUT_CLS}>
              <option>Online</option>
              <option>StaffDirect</option>
              <option>Other</option>
            </select>
          </Field>

          <button
            onClick={handleCalculate}
            disabled={loading}
            className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                       hover:bg-[#003370] disabled:opacity-60 transition flex items-center justify-center gap-2"
          >
            {loading
              ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              : <BarChart3 size={15} />}
            {loading ? 'Calculating…' : 'Calculate Benefits'}
          </button>

          {error && <ErrorBanner message={error} />}
        </div>
      </div>

      {/* Results */}
      <div className="lg:col-span-2">
        {result ? <ResultSection result={result} /> : (
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] flex items-center justify-center h-64 text-slate-400 text-sm">
            Enter parameters and click <strong className="mx-1">Calculate Benefits</strong> to see results.
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Shared result section ───────────────────────────────────────────────────

function ResultSection({ result }: { result: YpygResult }) {
  return (
    <div className="space-y-6">
      {/* Summary cards */}
      <div className="grid sm:grid-cols-3 gap-4">
        {[
          { label: 'Maturity Value', value: result.maturityValue, color: 'text-green-700', bg: 'bg-green-50 border-green-200' },
          { label: 'Surrender Value', value: result.surrenderValue, color: 'text-[#004282]', bg: 'bg-blue-50 border-blue-200' },
          { label: 'Death Benefit', value: result.deathBenefit, color: 'text-[#d32f2f]', bg: 'bg-red-50 border-red-200' },
        ].map(c => (
          <div key={c.label} className={`rounded-xl p-4 border ${c.bg}`}>
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">{c.label}</p>
            <p className={`text-2xl font-extrabold ${c.color}`}>₹ {INR(c.value)}</p>
          </div>
        ))}
      </div>

      {/* Additional info */}
      <div className="grid sm:grid-cols-2 gap-4">
        <InfoCard label="Sum Assured on Death" value={`₹ ${INR(result.sumAssuredOnDeath)}`} />
        <InfoCard label="Max Loan Amount" value={`₹ ${INR(result.maxLoanAmount)}`} />
      </div>

      {/* Yearly table */}
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100">
          <h3 className="text-base font-bold text-[#004282]">
            Yearly Benefit Table
            <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
          </h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-xs">
            <thead>
              <tr className="bg-blue-50/60 text-slate-500 uppercase tracking-wider">
                {['Yr', 'Annual Prem.', 'Total Paid', 'Guar. Income', 'Loyalty Inc.', 'Total Inc.', 'SV', 'Death Benefit', 'Maturity'].map(h => (
                  <th key={h} className="px-4 py-3 text-right first:text-center">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {result.yearlyTable.map(row => (
                <tr key={row.policyYear} className="hover:bg-slate-50 text-slate-700">
                  <td className="px-4 py-2 text-center font-semibold text-[#004282]">{row.policyYear}</td>
                  <td className="px-4 py-2 text-right">{INR(row.annualPremium)}</td>
                  <td className="px-4 py-2 text-right">{INR(row.totalPremiumsPaid)}</td>
                  <td className="px-4 py-2 text-right">{INR(row.guaranteedIncome)}</td>
                  <td className="px-4 py-2 text-right">{INR(row.loyaltyIncome)}</td>
                  <td className="px-4 py-2 text-right">{INR(row.totalIncome)}</td>
                  <td className="px-4 py-2 text-right">{INR(row.surrenderValue)}</td>
                  <td className="px-4 py-2 text-right text-[#d32f2f] font-semibold">{INR(row.deathBenefit)}</td>
                  <td className="px-4 py-2 text-right text-green-700 font-bold">{row.maturityBenefit > 0 ? INR(row.maturityBenefit) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// ─── Helper components ───────────────────────────────────────────────────────

const INPUT_CLS =
  'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#007bff]';

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-semibold text-slate-600 mb-1">{label}</label>
      {children}
    </div>
  );
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white rounded-xl shadow-[0_4px_15px_rgb(0,0,0,0.06)] p-4 border border-slate-100">
      <p className="text-xs text-slate-400 uppercase tracking-wider">{label}</p>
      <p className="text-lg font-bold text-[#004282] mt-1">{value}</p>
    </div>
  );
}

function ErrorBanner({ message }: { message: string }) {
  return (
    <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700">
      <AlertCircle size={14} className="mt-0.5 flex-shrink-0" />
      {message}
    </div>
  );
}

// ─── Main export — mode switcher ─────────────────────────────────────────────

export type YpygMode = 'policy-number' | 'input-value';

export default function YpygModule({ mode }: { mode: YpygMode }) {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          YPYG — {mode === 'policy-number' ? 'Policy Number' : 'Input Value'}
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          {mode === 'policy-number'
            ? 'Search by policy number to auto-populate fields and calculate benefits.'
            : 'Manually enter policy parameters to calculate maturity value, surrender value and death benefit.'}
        </p>
      </div>

      {mode === 'policy-number' ? <PolicyNumberMode /> : <InputValueMode />}
    </div>
  );
}

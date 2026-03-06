import { useState } from 'react';
import { ShieldCheck, PlusCircle, AlertCircle, CheckCircle2, XCircle, Upload } from 'lucide-react';
import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';
const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 2 });
const INPUT_CLS = 'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#007bff]';

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-semibold text-slate-600 mb-1">{label}</label>
      {children}
    </div>
  );
}

// ─── Payout Verification ─────────────────────────────────────────────────────

interface PayoutForm {
  policyNumber: string;
  annualPremium: number;
  policyTerm: number;
  premiumPayingTerm: number;
  premiumsPaid: number;
  entryAge: number;
  option: string;
  systemPayout: number;
}

interface PayoutResult {
  policyNumber: string;
  expectedPayout: number;
  systemPayout: number;
  variance: number;
  variancePct: number;
  status: string;
}

const DEFAULT_PAYOUT: PayoutForm = {
  policyNumber: '',
  annualPremium: 50000,
  policyTerm: 20,
  premiumPayingTerm: 10,
  premiumsPaid: 5,
  entryAge: 35,
  option: 'Immediate',
  systemPayout: 0,
};

function PayoutVerification() {
  const [form, setForm] = useState<PayoutForm>(DEFAULT_PAYOUT);
  const [result, setResult] = useState<PayoutResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const set = (k: keyof PayoutForm, v: string | number) => setForm(f => ({ ...f, [k]: v }));

  const handleVerify = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const res = await axios.post(`${API_URL}/api/audit/payout/single`, form);
      setResult(res.data);
    } catch (e: any) {
      setError(e?.response?.data?.error || 'Verification failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Payout Verification
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          Verify the expected payout against the system payout. Uses the existing Century Income benefit formulas.
        </p>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        {/* Form */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">Policy Details</h3>
            <Field label="Policy Number">
              <input type="text" value={form.policyNumber}
                onChange={e => set('policyNumber', e.target.value)} className={INPUT_CLS} placeholder="Optional" />
            </Field>
            <Field label="Annual Premium (₹)">
              <input type="number" value={form.annualPremium}
                onChange={e => set('annualPremium', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Policy Term (yrs)">
              <input type="number" value={form.policyTerm}
                onChange={e => set('policyTerm', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Premium Paying Term (yrs)">
              <input type="number" value={form.premiumPayingTerm}
                onChange={e => set('premiumPayingTerm', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Premiums Paid">
              <input type="number" value={form.premiumsPaid}
                onChange={e => set('premiumsPaid', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Entry Age">
              <input type="number" value={form.entryAge}
                onChange={e => set('entryAge', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Option">
              <select value={form.option} onChange={e => set('option', e.target.value)} className={INPUT_CLS}>
                <option>Immediate</option>
                <option>Deferred</option>
                <option>Twin</option>
              </select>
            </Field>
            <Field label="System Payout (₹)">
              <input type="number" value={form.systemPayout}
                onChange={e => set('systemPayout', +e.target.value)} className={INPUT_CLS} placeholder="Enter system payout to compare" />
            </Field>

            <button
              onClick={handleVerify}
              disabled={loading}
              className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                         hover:bg-[#003370] disabled:opacity-60 transition flex items-center justify-center gap-2"
            >
              {loading
                ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                : <ShieldCheck size={15} />}
              {loading ? 'Verifying…' : 'Verify Payout'}
            </button>

            {error && <ErrBanner msg={error} />}
          </div>
        </div>

        {/* Result */}
        <div className="lg:col-span-2">
          {result ? (
            <PayoutResultCard result={result} />
          ) : (
            <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] flex items-center justify-center h-64 text-slate-400 text-sm">
              Enter details and click <strong className="mx-1">Verify Payout</strong>.
            </div>
          )}

          {/* Excel Upload hint */}
          <div className="mt-6 bg-blue-50 border border-blue-100 rounded-xl p-4 flex items-start gap-3">
            <Upload size={18} className="text-[#004282] mt-0.5 flex-shrink-0" />
            <div>
              <p className="text-sm font-semibold text-[#004282]">Batch Verification</p>
              <p className="text-xs text-slate-500 mt-1">
                Upload an Excel file with columns: PolicyNumber, AnnualPremium, PolicyTerm,
                PremiumPayingTerm, PremiumsPaid, EntryAge, Option, SystemPayout.
                The system will verify each row automatically.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function PayoutResultCard({ result }: { result: PayoutResult }) {
  const isMatch = result.status === 'Match';
  return (
    <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-5">
      <div className="flex items-center gap-3">
        {isMatch
          ? <CheckCircle2 size={24} className="text-green-600" />
          : <XCircle size={24} className="text-[#d32f2f]" />}
        <div>
          <h3 className="text-lg font-bold text-[#004282]">Verification Result</h3>
          <span className={`inline-block px-3 py-0.5 rounded-full text-xs font-bold mt-1 ${
            isMatch ? 'bg-green-100 text-green-700' : 'bg-red-100 text-[#d32f2f]'
          }`}>
            {result.status}
          </span>
        </div>
      </div>

      <div className="grid sm:grid-cols-2 gap-4 text-sm">
        {[
          { label: 'Policy Number', value: result.policyNumber || '—' },
          { label: 'Expected Payout', value: `₹ ${INR(result.expectedPayout)}` },
          { label: 'System Payout', value: `₹ ${INR(result.systemPayout)}` },
          { label: 'Variance', value: `₹ ${INR(result.variance)}` },
          { label: 'Variance %', value: `${result.variancePct.toFixed(2)} %` },
        ].map(row => (
          <div key={row.label} className="bg-slate-50 rounded-lg p-3">
            <p className="text-xs text-slate-400 uppercase tracking-wider">{row.label}</p>
            <p className="font-bold text-[#004282] mt-0.5">{row.value}</p>
          </div>
        ))}
      </div>

      {!isMatch && (
        <div className="flex items-start gap-2 p-3 bg-amber-50 border border-amber-200 rounded-xl text-xs text-amber-700">
          <AlertCircle size={14} className="mt-0.5 flex-shrink-0" />
          Variance of {Math.abs(result.variancePct).toFixed(2)}% exceeds the 1% tolerance threshold.
          Please review the policy parameters.
        </div>
      )}
    </div>
  );
}

// ─── Addition / Bonus ────────────────────────────────────────────────────────

interface BonusForm {
  policyNumber: string;
  annualPremium: number;
  policyTerm: number;
  premiumPayingTerm: number;
  policyYear: number;
  entryAge: number;
  option: string;
  existingFundValue: number;
}

interface BonusResult {
  policyNumber: string;
  policyYear: number;
  bonusAddition: number;
  additionalBenefit: number;
  totalWithBonus: number;
  guaranteedIncome: number;
  loyaltyIncome: number;
}

const DEFAULT_BONUS: BonusForm = {
  policyNumber: '',
  annualPremium: 50000,
  policyTerm: 20,
  premiumPayingTerm: 10,
  policyYear: 5,
  entryAge: 35,
  option: 'Immediate',
  existingFundValue: 0,
};

function AdditionBonus() {
  const [form, setForm] = useState<BonusForm>(DEFAULT_BONUS);
  const [result, setResult] = useState<BonusResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const set = (k: keyof BonusForm, v: string | number) => setForm(f => ({ ...f, [k]: v }));

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const res = await axios.post(`${API_URL}/api/audit/bonus/single`, form);
      setResult(res.data);
    } catch (e: any) {
      setError(e?.response?.data?.error || 'Calculation failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Addition / Bonus
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          Calculate loyalty income addition and bonus for a specific policy year.
        </p>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        <div className="lg:col-span-1">
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">Parameters</h3>
            <Field label="Policy Number">
              <input type="text" value={form.policyNumber}
                onChange={e => set('policyNumber', e.target.value)} className={INPUT_CLS} placeholder="Optional" />
            </Field>
            <Field label="Annual Premium (₹)">
              <input type="number" value={form.annualPremium}
                onChange={e => set('annualPremium', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Policy Term (yrs)">
              <input type="number" value={form.policyTerm}
                onChange={e => set('policyTerm', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Premium Paying Term (yrs)">
              <input type="number" value={form.premiumPayingTerm}
                onChange={e => set('premiumPayingTerm', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Policy Year">
              <input type="number" value={form.policyYear}
                onChange={e => set('policyYear', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Entry Age">
              <input type="number" value={form.entryAge}
                onChange={e => set('entryAge', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Option">
              <select value={form.option} onChange={e => set('option', e.target.value)} className={INPUT_CLS}>
                <option>Immediate</option>
                <option>Deferred</option>
                <option>Twin</option>
              </select>
            </Field>
            <Field label="Existing Fund Value (₹)">
              <input type="number" value={form.existingFundValue}
                onChange={e => set('existingFundValue', +e.target.value)} className={INPUT_CLS} />
            </Field>

            <button
              onClick={handleCalculate}
              disabled={loading}
              className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                         hover:bg-[#003370] disabled:opacity-60 transition flex items-center justify-center gap-2"
            >
              {loading
                ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                : <PlusCircle size={15} />}
              {loading ? 'Calculating…' : 'Calculate Bonus'}
            </button>

            {error && <ErrBanner msg={error} />}
          </div>
        </div>

        <div className="lg:col-span-2">
          {result ? (
            <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-5">
              <h3 className="text-lg font-bold text-[#004282]">
                Bonus Result — Year {result.policyYear}
                <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
              </h3>
              <div className="grid sm:grid-cols-2 gap-4">
                {[
                  { label: 'Guaranteed Income', value: `₹ ${INR(result.guaranteedIncome)}`, color: 'text-[#004282]' },
                  { label: 'Loyalty Income (Bonus)', value: `₹ ${INR(result.loyaltyIncome)}`, color: 'text-indigo-600' },
                  { label: 'Bonus Addition', value: `₹ ${INR(result.bonusAddition)}`, color: 'text-[#007bff]' },
                  { label: 'Additional Benefit', value: `₹ ${INR(result.additionalBenefit)}`, color: 'text-[#004282]' },
                  { label: 'Total With Bonus', value: `₹ ${INR(result.totalWithBonus)}`, color: 'text-green-700' },
                ].map(row => (
                  <div key={row.label} className="bg-slate-50 rounded-xl p-4">
                    <p className="text-xs text-slate-400 uppercase tracking-wider">{row.label}</p>
                    <p className={`text-xl font-extrabold mt-1 ${row.color}`}>{row.value}</p>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] flex items-center justify-center h-64 text-slate-400 text-sm">
              Enter parameters and click <strong className="mx-1">Calculate Bonus</strong>.
            </div>
          )}

          <div className="mt-6 bg-blue-50 border border-blue-100 rounded-xl p-4 flex items-start gap-3">
            <Upload size={18} className="text-[#004282] mt-0.5 flex-shrink-0" />
            <div>
              <p className="text-sm font-semibold text-[#004282]">Batch Bonus Calculation</p>
              <p className="text-xs text-slate-500 mt-1">
                Upload an Excel file with columns: PolicyNumber, AnnualPremium, PolicyTerm,
                PremiumPayingTerm, PolicyYear, EntryAge, Option, ExistingFundValue.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// ─── Shared error banner ──────────────────────────────────────────────────────

function ErrBanner({ msg }: { msg: string }) {
  return (
    <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700">
      <AlertCircle size={14} className="mt-0.5 flex-shrink-0" />
      {msg}
    </div>
  );
}

// ─── Main export ─────────────────────────────────────────────────────────────

export type AuditSubModule = 'payout-verification' | 'addition-bonus';

export default function AuditModule({ sub }: { sub: AuditSubModule }) {
  return sub === 'payout-verification' ? <PayoutVerification /> : <AdditionBonus />;
}

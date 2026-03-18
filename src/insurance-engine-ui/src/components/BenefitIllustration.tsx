import { useState, useEffect } from 'react';
import { TrendingUp, AlertCircle, Info, FileDown, User, Settings2 } from 'lucide-react';
import { runBenefitIllustration, getEndowmentConfig } from '../api';
import type { BenefitIllustrationResult, BenefitIllustrationRequest, EndowmentProductConfig } from '../api';
import { downloadEndowmentBiPdf } from '../utils/pdfExport';

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 });
const INPUT_CLS = `w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                   focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]
                   placeholder:text-slate-300`;

/* Fallback config used until the backend responds */
const DEFAULT_CONFIG: EndowmentProductConfig = {
  pptOptions: [7, 10, 12],
  ptOptionsByPpt: { '7': [15, 20], '10': [20, 25], '12': [25] },
  channels: ['Corporate Agency', 'Direct Marketing', 'Online', 'Broker', 'Agency', 'Web Aggregator', 'Insurance Marketing Firm'],
  paymentModes: ['Yearly', 'Half Yearly', 'Quarterly', 'Monthly'],
};

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-semibold text-slate-600 mb-1">{label}</label>
      {children}
    </div>
  );
}

export default function BenefitIllustration() {
  const [config, setConfig] = useState<EndowmentProductConfig>(DEFAULT_CONFIG);
  const [form, setForm] = useState<BenefitIllustrationRequest>({
    annualisedPremium: 50000,
    annualPremium: 50000,
    ppt: 7,
    policyTerm: 15,
    entryAge: 35,
    nameOfLifeAssured: '',
    nameOfPolicyHolder: '',
    ageOfPolicyHolder: undefined,
    option: 'Immediate',
    channel: 'Agency',
    gender: 'Male',
    premiumFrequency: 'Yearly',
    standardAgeProof: false,
    staffPolicy: false,
    isPreIssuance: true,
  });
  const [result, setResult] = useState<BenefitIllustrationResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  /* Load product config from backend on mount */
  useEffect(() => {
    getEndowmentConfig()
      .then(r => setConfig(r.data))
      .catch(() => { /* keep fallback */ });
  }, []);

  const set = (k: keyof BenefitIllustrationRequest, v: any) => setForm(p => ({ ...p, [k]: v }));

  /* When PPT changes, reset Policy Term to first valid option */
  const handlePptChange = (newPpt: number) => {
    const ptOptions = config.ptOptionsByPpt[String(newPpt)] ?? [];
    setForm(p => ({
      ...p,
      ppt: newPpt,
      policyTerm: ptOptions.length > 0 ? ptOptions[0] : p.policyTerm,
    }));
  };

  const ptOptions = config.ptOptionsByPpt[String(form.ppt)] ?? [];

  const handleCalculate = async () => {
    setLoading(true); setError(null); setResult(null);
    try {
      const resp = await runBenefitIllustration(form);
      setResult(resp.data);
    } catch (e: any) {
      const msg = e.response?.data || e.message;
      setError(typeof msg === 'string' && msg
        ? msg
        : 'Unable to generate illustration. Please verify all fields and try again.');
    } finally { setLoading(false); }
  };

  return (
    <div className="space-y-6">
      {/* Page heading */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Endowment — Benefit Illustration
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          Pre-issuance illustration. Enter policyholder details and plan parameters, then generate the yearly benefit table.
        </p>
      </div>

      {/* ── 2-Section input layout ── */}
      <div className="grid lg:grid-cols-2 gap-6">
        {/* Section 1 — Policyholder Details */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <div className="flex items-center gap-2 mb-2">
            <User size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Policyholder Details</h3>
          </div>

          <Field label="Name of the Life Assured">
            <input type="text" value={form.nameOfLifeAssured ?? ''}
              onChange={e => set('nameOfLifeAssured', e.target.value)}
              placeholder="Enter name" className={INPUT_CLS} />
          </Field>

          <Field label="Age of the Life Assured (years)">
            <input type="number" value={form.entryAge}
              onChange={e => set('entryAge', +e.target.value)} className={INPUT_CLS} />
          </Field>

          <Field label="Name of the Policy Holder">
            <input type="text" value={form.nameOfPolicyHolder ?? ''}
              onChange={e => set('nameOfPolicyHolder', e.target.value)}
              placeholder="Enter name" className={INPUT_CLS} />
          </Field>

          <Field label="Age of the Policy Holder (years)">
            <input type="number" value={form.ageOfPolicyHolder ?? ''}
              onChange={e => set('ageOfPolicyHolder', e.target.value === '' ? undefined : +e.target.value)}
              placeholder="Enter age" className={INPUT_CLS} />
          </Field>

          <Field label="Gender">
            <select value={form.gender ?? 'Male'}
              onChange={e => set('gender', e.target.value as 'Male' | 'Female')}
              className={INPUT_CLS}>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
            </select>
          </Field>

          <Field label="Sum Assured (₹) — optional override">
            <input type="number" value={form.sumAssured ?? ''} placeholder="Auto: Annual Premium × 10"
              onChange={e => set('sumAssured', e.target.value === '' ? undefined : +e.target.value)}
              className={INPUT_CLS} />
          </Field>

          <Field label="Standard Age Proof">
            <select value={form.standardAgeProof ? 'Yes' : 'No'}
              onChange={e => set('standardAgeProof', e.target.value === 'Yes')}
              className={INPUT_CLS}>
              <option value="Yes">Yes</option>
              <option value="No">No</option>
            </select>
          </Field>

          <div className="pt-2 border-t border-slate-100">
            <p className="text-xs text-slate-400">
              <strong>SA</strong> = 10 × Annual Premium &nbsp;·&nbsp;
              <strong>SV</strong> = Max(GSV, SSV) &nbsp;·&nbsp;
              <strong>DB</strong> = Max(SA, SV, 105% × TPP)
            </p>
          </div>
        </div>

        {/* Section 2 — Product Selection / Plan Parameters */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <div className="flex items-center gap-2 mb-2">
            <Settings2 size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Plan Parameters</h3>
          </div>

          <Field label="Annualised Premium (₹)">
            <input type="number" value={form.annualisedPremium ?? form.annualPremium}
              onChange={e => {
                const val = +e.target.value;
                setForm(p => ({ ...p, annualisedPremium: val, annualPremium: val }));
              }} className={INPUT_CLS} />
          </Field>

          <Field label="Premium Payment Mode">
            <select value={form.premiumFrequency ?? 'Yearly'}
              onChange={e => set('premiumFrequency', e.target.value as BenefitIllustrationRequest['premiumFrequency'])}
              className={INPUT_CLS}>
              {config.paymentModes.map(m => (
                <option key={m} value={m}>{m}</option>
              ))}
            </select>
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label="PPT (years)">
              <select value={form.ppt}
                onChange={e => handlePptChange(+e.target.value)}
                className={INPUT_CLS}>
                {config.pptOptions.map(p => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </Field>
            <Field label="Policy Term (years)">
              <select value={form.policyTerm}
                onChange={e => set('policyTerm', +e.target.value)}
                className={INPUT_CLS}>
                {ptOptions.map(p => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </Field>
          </div>

          <Field label="Income Option">
            <select value={form.option}
              onChange={e => set('option', e.target.value as BenefitIllustrationRequest['option'])}
              className={INPUT_CLS}>
              <option value="Immediate">Immediate</option>
              <option value="Deferred">Deferred</option>
              <option value="Twin">Twin Income</option>
            </select>
          </Field>

          <Field label="Sales Channel">
            <select value={form.channel}
              onChange={e => set('channel', e.target.value)}
              className={INPUT_CLS}>
              {config.channels.map(ch => (
                <option key={ch} value={ch}>{ch}</option>
              ))}
            </select>
          </Field>

          <Field label="Staff Policy">
            <select value={form.staffPolicy ? 'Yes' : 'No'}
              onChange={e => set('staffPolicy', e.target.value === 'Yes')}
              className={INPUT_CLS}>
              <option value="No">No</option>
              <option value="Yes">Yes</option>
            </select>
          </Field>

          <div className="flex items-center gap-2 text-xs text-slate-500 pt-1">
            <input type="checkbox" id="preIssuance" checked={form.isPreIssuance ?? true}
              onChange={e => set('isPreIssuance', e.target.checked)}
              className="accent-[#004282]" />
            <label htmlFor="preIssuance">Pre-issuance (skip policy date logic)</label>
          </div>

          <button
            onClick={handleCalculate}
            disabled={loading}
            className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                       hover:bg-[#003370] disabled:opacity-50 transition-colors
                       flex items-center justify-center gap-2"
          >
            {loading
              ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              : <TrendingUp size={16} />}
            {loading ? 'Calculating…' : 'Generate Illustration'}
          </button>

          {error && (
            <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-xs text-red-700">
              <AlertCircle size={14} className="mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}
        </div>
      </div>

      {/* ── Results ── */}
      {result && (
        <>
          {/* Summary cards — SA on Maturity is intentionally omitted per requirements */}
          <div className="grid sm:grid-cols-3 gap-4">
            {[
              { label: 'Sum Assured on Death', value: result.sumAssuredOnDeath, color: 'text-[#004282]' },
              { label: 'Annual Premium (Calculated)', value: result.annualPremium, color: 'text-[#007bff]' },
              { label: 'Max Loan Amount (70% SV)', value: result.maxLoanAmount, color: 'text-[#004282]' },
            ].map(m => (
              <div key={m.label} className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5">
                <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{m.label}</p>
                <p className={`text-2xl font-extrabold mt-1 ${m.color}`}>₹ {INR(m.value)}</p>
              </div>
            ))}
          </div>

          {/* Table with PDF export */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
              <h3 className="text-base font-bold text-[#004282]">
                Yearly Benefit Table
                <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
              </h3>
              <span className="flex items-center gap-1 text-xs text-slate-400 ml-2">
                <Info size={12} /> ₹
              </span>
              <button
                onClick={() => downloadEndowmentBiPdf(result)}
                className="ml-auto flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold
                           bg-[#004282] text-white rounded-lg hover:bg-[#003370] transition"
              >
                <FileDown size={13} />
                Download PDF
              </button>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider whitespace-nowrap">
                    <th className="px-3 py-3 text-center">PY</th>
                    <th className="px-3 py-3 text-right">Premium</th>
                    <th className="px-3 py-3 text-right">Guar. Income</th>
                    <th className="px-3 py-3 text-right">Loyalty Inc.</th>
                    <th className="px-3 py-3 text-right">Total Inc.</th>
                    <th className="px-3 py-3 text-right">Cum. Benefits</th>
                    <th className="px-3 py-3 text-right">GSV</th>
                    <th className="px-3 py-3 text-right">SSV</th>
                    <th className="px-3 py-3 text-right font-bold text-[#004282]">Surr. Value</th>
                    <th className="px-3 py-3 text-right">Death Benefit</th>
                    <th className="px-3 py-3 text-right text-[#d32f2f]">Maturity</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {result.yearlyTable.map(row => (
                    <tr
                      key={row.policyYear}
                      className={`
                        hover:bg-slate-50 text-slate-700 transition-colors
                        ${row.maturityBenefit > 0 ? 'bg-green-50/30 font-semibold' : ''}
                        ${row.isPaidUp ? 'bg-amber-50/30' : ''}
                      `}
                    >
                      <td className="px-3 py-2.5 text-center font-bold text-[#004282]">{row.policyYear}</td>
                      <td className="px-3 py-2.5 text-right font-mono">{row.annualPremium > 0 ? INR(row.annualPremium) : '—'}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-green-700">{row.guaranteedIncome > 0 ? INR(row.guaranteedIncome) : '—'}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-green-600">{row.loyaltyIncome > 0 ? INR(row.loyaltyIncome) : '—'}</td>
                      <td className="px-3 py-2.5 text-right font-mono font-semibold">{INR(row.totalIncome)}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-slate-400">{INR(row.cumulativeSurvivalBenefits)}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-slate-500">{INR(row.gsv)}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-slate-500">{INR(row.ssv)}</td>
                      <td className="px-3 py-2.5 text-right font-mono font-bold text-[#004282]">{INR(row.surrenderValue)}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-slate-600">{INR(row.deathBenefit)}</td>
                      <td className="px-3 py-2.5 text-right font-mono font-extrabold text-[#d32f2f]">
                        {row.maturityBenefit > 0 ? INR(row.maturityBenefit) : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

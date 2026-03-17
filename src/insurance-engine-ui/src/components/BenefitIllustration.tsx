import { useState } from 'react';
import { TrendingUp, AlertCircle, Info, FileDown, User, Settings2 } from 'lucide-react';
import { runBenefitIllustration } from '../api';
import type { BenefitIllustrationResult, BenefitIllustrationRequest } from '../api';
import { downloadEndowmentBiPdf } from '../utils/pdfExport';

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 });
const INPUT_CLS = `w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                   focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]
                   placeholder:text-slate-300`;

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-semibold text-slate-600 mb-1">{label}</label>
      {children}
    </div>
  );
}

export default function BenefitIllustration() {
  const [form, setForm] = useState<BenefitIllustrationRequest>({
    annualPremium: 50000,
    ppt: 7,
    policyTerm: 15,
    entryAge: 35,
    option: 'Immediate',
    channel: 'Other',
    gender: 'Male',
    premiumFrequency: 'Yearly',
    standardAgeProof: false,
    isPreIssuance: true,
  });
  const [result, setResult] = useState<BenefitIllustrationResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const set = (k: keyof BenefitIllustrationRequest, v: any) => setForm(p => ({ ...p, [k]: v }));

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

          <Field label="Entry Age (years)">
            <input type="number" value={form.entryAge}
              onChange={e => set('entryAge', +e.target.value)} className={INPUT_CLS} />
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
            <input type="number" value={form.sumAssured ?? ''} placeholder="Auto-derived from GMB"
              onChange={e => set('sumAssured', e.target.value === '' ? undefined : +e.target.value)}
              className={INPUT_CLS} />
          </Field>

          <Field label="Premiums Paid (for Paid-Up calculation)">
            <input type="number" value={form.premiumsPaid ?? ''} placeholder="Leave blank if fully paid-up"
              onChange={e => {
                const v = e.target.value;
                set('premiumsPaid', v === '' ? undefined : +v);
              }} className={INPUT_CLS} />
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
              <strong>SAD</strong> = Max(10 × AP, GMB) &nbsp;·&nbsp;
              <strong>SV</strong> = Max(GSV, SSV) &nbsp;·&nbsp;
              <strong>DB</strong> = Max(SAD, SV, 105% × TPP)
            </p>
          </div>
        </div>

        {/* Section 2 — Product Selection / Plan Parameters */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <div className="flex items-center gap-2 mb-2">
            <Settings2 size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Plan Parameters</h3>
          </div>

          <Field label="Annual Premium — AP (₹)">
            <input type="number" value={form.annualPremium}
              onChange={e => set('annualPremium', +e.target.value)} className={INPUT_CLS} />
          </Field>

          <Field label="Premium Payment Mode">
            <select value={form.premiumFrequency ?? 'Yearly'}
              onChange={e => set('premiumFrequency', e.target.value as BenefitIllustrationRequest['premiumFrequency'])}
              className={INPUT_CLS}>
              <option value="Yearly">Yearly</option>
              <option value="Half Yearly">Half Yearly</option>
              <option value="Quarterly">Quarterly</option>
              <option value="Monthly">Monthly</option>
            </select>
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label="PPT (years)">
              <input type="number" value={form.ppt}
                onChange={e => set('ppt', +e.target.value)} className={INPUT_CLS} />
            </Field>
            <Field label="Policy Term (years)">
              <input type="number" value={form.policyTerm}
                onChange={e => set('policyTerm', +e.target.value)} className={INPUT_CLS} />
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
              onChange={e => set('channel', e.target.value as BenefitIllustrationRequest['channel'])}
              className={INPUT_CLS}>
              <option value="Other">Other / Direct</option>
              <option value="Online">Online (+4.25%)</option>
              <option value="StaffDirect">Staff Direct (+8.5%)</option>
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
          {/* Summary cards */}
          <div className="grid sm:grid-cols-3 gap-4">
            {[
              { label: 'Sum Assured on Death', value: result.sumAssuredOnDeath, color: 'text-[#004282]' },
              { label: 'Guaranteed Maturity Benefit', value: result.guaranteedMaturityBenefit, color: 'text-[#d32f2f]' },
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

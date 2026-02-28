import { useState } from 'react';
import { TrendingUp, AlertCircle, Info } from 'lucide-react';
import { runBenefitIllustration } from '../api';
import type { BenefitIllustrationResult, BenefitIllustrationRequest } from '../api';

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 });

export default function BenefitIllustration() {
  const [form, setForm] = useState<BenefitIllustrationRequest>({
    annualPremium: 50000,
    ppt: 7,
    policyTerm: 15,
    entryAge: 35,
    option: 'Immediate',
    channel: 'Other',
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
      setError(e.response?.data || e.message || 'Calculation failed');
    } finally { setLoading(false); }
  };

  return (
    <div className="space-y-8">
      {/* Heading */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Century Income — Benefit Illustration
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          Generate a complete yearly benefit illustration table including GI, LI, GSV, SSV, Death Benefit, and Maturity Benefit.
        </p>
      </div>

      <div className="grid lg:grid-cols-4 gap-8">
        {/* Input panel */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">Policy Details</h3>

            {[
              { label: 'Annual Premium (₹)', key: 'annualPremium' as const, type: 'number' },
              { label: 'PPT (Premium Payment Term)', key: 'ppt' as const, type: 'number' },
              { label: 'Policy Term (years)', key: 'policyTerm' as const, type: 'number' },
              { label: 'Entry Age', key: 'entryAge' as const, type: 'number' },
              { label: 'Premiums Paid (Paid-Up)', key: 'premiumsPaid' as const, type: 'number' },
            ].map(f => (
              <div key={f.key}>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">{f.label}</label>
                <input
                  type="number"
                  value={form[f.key] ?? ''}
                  onChange={e => set(f.key, e.target.value ? parseFloat(e.target.value) : undefined)}
                  placeholder={f.key === 'premiumsPaid' ? 'Leave blank if fully paid' : undefined}
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                             focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]
                             placeholder:text-slate-300"
                />
              </div>
            ))}

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Income Option</label>
              <select
                value={form.option}
                onChange={e => set('option', e.target.value as BenefitIllustrationRequest['option'])}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]"
              >
                <option value="Immediate">Immediate</option>
                <option value="Deferred">Deferred</option>
                <option value="Twin">Twin</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Sales Channel</label>
              <select
                value={form.channel}
                onChange={e => set('channel', e.target.value as BenefitIllustrationRequest['channel'])}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]"
              >
                <option value="Other">Other / Direct</option>
                <option value="Online">Online (+4.25%)</option>
                <option value="StaffDirect">Staff Direct (+8.5%)</option>
              </select>
            </div>

            <button
              onClick={handleCalculate}
              disabled={loading}
              className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                         hover:bg-[#003370] disabled:opacity-50 transition-colors
                         flex items-center justify-center gap-2"
            >
              {loading ? (
                <span className="inline-block w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <TrendingUp size={16} />
              )}
              {loading ? 'Calculating…' : 'Generate Illustration'}
            </button>

            {error && (
              <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-xs text-red-700">
                <AlertCircle size={14} className="mt-0.5 flex-shrink-0" />
                <span>{typeof error === 'string' ? error : 'Calculation failed'}</span>
              </div>
            )}
          </div>
        </div>

        {/* Results panel */}
        <div className="lg:col-span-3 space-y-6">
          {/* Summary metric cards */}
          {result && (
            <div className="grid sm:grid-cols-3 gap-4">
              {[
                { label: 'Sum Assured on Death', value: result.sumAssuredOnDeath, highlight: false },
                { label: 'Guaranteed Maturity Benefit', value: result.guaranteedMaturityBenefit, highlight: true },
                { label: 'Max Loan Amount (70% SV)', value: result.maxLoanAmount, highlight: false },
              ].map(m => (
                <div key={m.label} className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5">
                  <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{m.label}</p>
                  <p className={`text-2xl font-extrabold mt-1 ${m.highlight ? 'text-[#d32f2f]' : 'text-[#004282]'}`}>
                    ₹ {INR(m.value)}
                  </p>
                </div>
              ))}
            </div>
          )}

          {/* Yearly table */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-2">
              <h3 className="text-base font-bold text-[#004282]">
                Yearly Benefit Table
                <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
              </h3>
              {result && (
                <span className="ml-auto flex items-center gap-1 text-xs text-slate-400">
                  <Info size={12} /> All values in ₹
                </span>
              )}
            </div>
            {!result && !loading && (
              <div className="px-6 py-16 text-center text-slate-400 text-sm">
                Fill in policy details and click <strong>Generate Illustration</strong>.
              </div>
            )}
            {result && (
              <div className="overflow-x-auto">
                <table className="w-full text-xs">
                  <thead>
                    <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider whitespace-nowrap">
                      <th className="px-3 py-3 text-center sticky left-0 bg-blue-50/50">PY</th>
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
                        <td className="px-3 py-2.5 text-center font-bold text-[#004282] sticky left-0 bg-inherit">
                          {row.policyYear}
                        </td>
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
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

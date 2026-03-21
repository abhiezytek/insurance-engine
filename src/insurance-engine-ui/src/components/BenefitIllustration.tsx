import { useEffect, useMemo, useState } from 'react';
import { TrendingUp, AlertCircle, Info, FileDown, User, Settings2 } from 'lucide-react';
import { runBenefitIllustration, getEndowmentConfig } from '../api';
import type { BenefitIllustrationResult, BenefitIllustrationRequest, EndowmentProductConfig } from '../api';
import { downloadEndowmentBiPdf } from '../utils/pdfExport';
import {
  MODAL_FACTORS,
  calculateAge,
  deriveCenturyIncomeValues,
  getCenturyIncomePtOptions,
  type CenturyIncomeForm,
  type PremiumFrequency,
  type SalesChannel,
  type Gender,
  validateCenturyIncome,
} from '../utils/biRules';

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 });
const INPUT_CLS = `w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                    focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]
                    placeholder:text-slate-300`;

/* Fallback config used until the backend responds */
const DEFAULT_CONFIG: EndowmentProductConfig = {
  pptOptions: [7, 10, 12],
  ptOptionsByPpt: { '7': [15, 20], '10': [20, 25], '12': [25] },
  channels: ['Corporate Agency', 'Direct Marketing', 'Online', 'Broker', 'Agency', 'Web Aggregator', 'Insurance Marketing Firm'],
  paymentModes: ['Yearly', 'Half-Yearly', 'Quarterly', 'Monthly'],
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
  const [form, setForm] = useState<CenturyIncomeForm>({
    product: 'CENTURY_INCOME',
    option: 'Immediate',
    isProposerDifferent: false,
    lifeAssuredName: '',
    proposerName: '',
    lifeAssuredDob: null,
    proposerDob: null,
    lifeAssuredGender: null,
    proposerGender: null,
    lifeAssuredAge: null,
    proposerAge: null,
    premium: null,
    premiumFrequency: 'Yearly',
    annualisedPremium: null,
    sumAssured: null,
    ppt: 7,
    pt: 15,
    standardAgeProof: null,
    salesChannel: 'Agency',
    staffPolicy: false,
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

  const set = <K extends keyof CenturyIncomeForm>(k: K, v: CenturyIncomeForm[K]) =>
    setForm(p => ({ ...p, [k]: v }));

  /* When PPT changes, reset Policy Term to first valid option */
  const handlePptChange = (newPpt: number) => {
    const ptOptions = getCenturyIncomePtOptions(newPpt as any);
    setForm(p => ({
      ...p,
      ppt: newPpt as any,
      pt: ptOptions.length > 0 ? (ptOptions[0] as any) : p.pt,
    }));
  };

  const ptOptions = useMemo(() => getCenturyIncomePtOptions(form.ppt), [form.ppt]);

  const derived = useMemo(() => deriveCenturyIncomeValues(form), [form]);

  const handleCalculate = async () => {
    setLoading(true); setError(null); setResult(null);
    const lifeAssuredAge = calculateAge(form.lifeAssuredDob ?? null);
    const proposerAge = calculateAge(form.proposerDob ?? null);

    const nextForm: CenturyIncomeForm = {
      ...form,
      lifeAssuredAge,
      proposerAge,
      annualisedPremium: derived.annualisedPremium,
      sumAssured: derived.sumAssured,
    };

    const validationErrors = validateCenturyIncome(nextForm);
    if (validationErrors.length > 0) {
      setError(validationErrors.join('; '));
      setLoading(false);
      return;
    }

    try {
      const payload: BenefitIllustrationRequest = {
        annualisedPremium: nextForm.annualisedPremium ?? undefined,
        annualPremium: 0,
        ppt: nextForm.ppt ?? 0,
        policyTerm: nextForm.pt ?? 0,
        entryAge: lifeAssuredAge ?? undefined,
        dateOfBirth: nextForm.lifeAssuredDob ?? undefined,
        nameOfLifeAssured: nextForm.lifeAssuredName,
        nameOfPolicyHolder: nextForm.isProposerDifferent ? nextForm.proposerName : nextForm.lifeAssuredName,
        ageOfPolicyHolder: proposerAge ?? undefined,
        policyholderDateOfBirth: nextForm.isProposerDifferent ? nextForm.proposerDob ?? undefined : nextForm.lifeAssuredDob ?? undefined,
        lifeAssuredSameAsProposer: !nextForm.isProposerDifferent,
        option: nextForm.option === 'Twin Income' ? 'Twin' : (nextForm.option as any),
        channel: (nextForm.salesChannel ?? '') as string,
        gender: (nextForm.lifeAssuredGender ?? 'Male') as any,
        premiumFrequency: (nextForm.premiumFrequency === 'Half-Yearly' ? 'Half Yearly' : nextForm.premiumFrequency) as any,
        standardAgeProof: nextForm.standardAgeProof ?? undefined,
        staffPolicy: nextForm.staffPolicy ?? undefined,
        premiumsPaid: undefined,
        sumAssured: nextForm.sumAssured ?? undefined,
        isPreIssuance: true,
      };

      const resp = await runBenefitIllustration(payload);
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
          Enter proposer details and plan parameters, then generate the yearly benefit table.
        </p>
      </div>

      {/* ── 2-Section input layout ── */}
      <div className="grid lg:grid-cols-2 gap-6">
        {/* Section 1 — Product Selection / Plan Parameters */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5 space-y-4">
          <div className="flex items-center gap-2 mb-1">
            <Settings2 size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Plan Parameters</h3>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field label="Product">
              <select
                value={form.product}
                onChange={e => set('product', e.target.value as CenturyIncomeForm['product'])}
                className={INPUT_CLS}>
                <option value="CENTURY_INCOME">SUD Life Century Income</option>
              </select>
            </Field>

            <Field label="Option">
              <select value={form.option}
                onChange={e => set('option', e.target.value as CenturyIncomeForm['option'])}
                className={INPUT_CLS}>
                <option value="Immediate">Immediate</option>
                <option value="Deferred">Deferred</option>
                <option value="Twin Income">Twin Income</option>
              </select>
            </Field>

            <Field label="Premium Frequency">
              <select value={form.premiumFrequency ?? 'Yearly'}
                onChange={e => set('premiumFrequency', e.target.value as PremiumFrequency)}
                className={INPUT_CLS}>
                {config.paymentModes.map(m => (
                  <option key={m} value={m as PremiumFrequency}>{m}</option>
                ))}
              </select>
            </Field>

            <Field label="Premium (Installment)">
              <input
                type="number"
                value={form.premium ?? ''}
                onChange={e => set('premium', e.target.value ? Number(e.target.value) : null)}
                placeholder="Enter premium"
                className={INPUT_CLS}
              />
              {form.premiumFrequency && (
                <p className="text-[11px] text-slate-400 mt-1">
                  Modal factor: {MODAL_FACTORS[form.premiumFrequency as PremiumFrequency]}
                </p>
              )}
            </Field>

            <Field label="Annualised Premium (Derived)">
              <input
                type="number"
                value={(derived.annualisedPremium ?? 0).toFixed(0)}
                readOnly
                className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`}
              />
            </Field>

            <Field label="Sum Assured on Death (10 × Annualised Premium)">
              <input
                type="number"
                value={(result?.sumAssuredOnDeath ?? derived.sumAssured ?? 0).toFixed(0)}
                readOnly
                className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`}
              />
            </Field>

            <Field label="Premium Payment Term (PPT)">
              <select value={form.ppt ?? undefined}
                onChange={e => handlePptChange(+e.target.value)}
                className={INPUT_CLS}>
                <option value="">Select PPT</option>
                {config.pptOptions.map(p => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </Field>

            <Field label="Policy Term (PT)">
              <select value={form.pt ?? undefined}
                onChange={e => set('pt', (+e.target.value || null) as any)}
                className={INPUT_CLS}>
                <option value="">Select PT</option>
                {ptOptions.map(p => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </Field>

            <Field label="Sales Channel">
              <select value={form.salesChannel ?? ''}
                onChange={e => set('salesChannel', e.target.value as SalesChannel)}
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

            <Field label="Standard Age Proof">
              <select value={form.standardAgeProof ? 'Yes' : 'No'}
                onChange={e => set('standardAgeProof', e.target.value === 'Yes')}
                className={INPUT_CLS}>
                <option value="Yes">Yes</option>
                <option value="No">No</option>
              </select>
            </Field>
          </div>

          <button
            onClick={handleCalculate}
            disabled={loading}
            className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                       hover:bg-[#003370] disabled:opacity-50 transition-colors
                       flex items-center justify-center gap-2 mt-1"
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

        {/* Section 2 — Life Assured / Proposer */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5 space-y-4">
          <div className="flex items-center gap-2 mb-1">
            <User size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Life Assured & Proposer</h3>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field label="Life Assured Name">
              <input
                type="text"
                value={form.lifeAssuredName}
                onChange={e => set('lifeAssuredName', e.target.value)}
                placeholder="Life Assured Name"
                className={INPUT_CLS}
              />
            </Field>

            <Field label="Life Assured Gender">
              <select
                value={form.lifeAssuredGender ?? ''}
                onChange={e => set('lifeAssuredGender', e.target.value as Gender)}
                className={INPUT_CLS}>
                <option value="">Select</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Transgender">Transgender</option>
              </select>
            </Field>

            <Field label="Life Assured DOB">
              <input
                type="date"
                value={form.lifeAssuredDob ?? ''}
                onChange={e => set('lifeAssuredDob', e.target.value || null)}
                className={INPUT_CLS}
              />
            </Field>

            <Field label="Life Assured Age (auto)">
              <input
                type="number"
                value={calculateAge(form.lifeAssuredDob) ?? ''}
                readOnly
                className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`}
              />
            </Field>
          </div>

          <div className="flex items-center gap-2 text-xs text-slate-600">
            <input
              id="proposer-different"
              type="checkbox"
              checked={form.isProposerDifferent}
              onChange={e => {
                const next = e.target.checked;
                setForm(p => ({
                  ...p,
                  isProposerDifferent: next,
                  proposerName: next ? p.proposerName : '',
                  proposerDob: next ? p.proposerDob : null,
                  proposerAge: next ? p.proposerAge : null,
                  proposerGender: next ? p.proposerGender : null,
                }));
              }}
              className="accent-[#004282]"
            />
            <label htmlFor="proposer-different" className="cursor-pointer">
              Proposer is different from Life Assured
            </label>
          </div>

          {form.isProposerDifferent && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <Field label="Proposer Name">
                <input
                  type="text"
                  value={form.proposerName ?? ''}
                  onChange={e => set('proposerName', e.target.value)}
                  placeholder="Proposer Name"
                  className={INPUT_CLS}
                />
              </Field>

              <Field label="Proposer Gender">
                <select
                  value={form.proposerGender ?? ''}
                  onChange={e => set('proposerGender', e.target.value as Gender)}
                  className={INPUT_CLS}>
                  <option value="">Select</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Transgender">Transgender</option>
                </select>
              </Field>

              <Field label="Proposer DOB">
                <input
                  type="date"
                  value={form.proposerDob ?? ''}
                  onChange={e => set('proposerDob', e.target.value || null)}
                  className={INPUT_CLS}
                />
              </Field>

              <Field label="Proposer Age (auto)">
                <input
                  type="number"
                value={calculateAge(form.proposerDob ?? null) ?? ''}
                  readOnly
                  className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`}
                />
              </Field>
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
                    <th className="px-3 py-3 text-right">GSV Factor</th>
                    <th className="px-3 py-3 text-right">SSV F1</th>
                    <th className="px-3 py-3 text-right">SSV F2</th>
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
                      <td className="px-3 py-2.5 text-right font-mono text-slate-500">{row.gsvFactor?.toFixed(4) ?? '—'}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-slate-500">{row.ssvFactor1?.toFixed(4) ?? '—'}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-slate-500">{row.ssvFactor2?.toFixed(4) ?? '—'}</td>
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

            {/* GSV vs SSV summary */}
            <div className="px-6 py-4 border-t border-slate-100 text-xs text-slate-600 grid sm:grid-cols-3 gap-3 bg-slate-50/40">
              <div>
                <p className="font-semibold text-slate-500">a) Guaranteed Surrender Value</p>
                <p className="text-[#004282] font-bold">₹ {INR(result.yearlyTable.at(-1)?.gsv ?? 0)}</p>
              </div>
              <div>
                <p className="font-semibold text-slate-500">b) Special Surrender Value</p>
                <p className="text-[#004282] font-bold">₹ {INR(result.yearlyTable.at(-1)?.ssv ?? 0)}</p>
              </div>
              <div>
                <p className="font-semibold text-slate-500">Surrender Value [Greater of (a) or (b)]</p>
                <p className="text-[#d32f2f] font-extrabold">₹ {INR(result.yearlyTable.at(-1)?.surrenderValue ?? 0)}</p>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

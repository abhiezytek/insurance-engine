import { useState, useEffect, useMemo } from 'react';
import { TrendingUp, AlertCircle, Info, ChevronDown, ChevronUp, FileDown, User, Settings2 } from 'lucide-react';
import {
  getUlipProducts,
  runUlipCalculation,
  type UlipCalculationRequest,
  type UlipCalculationResult,
  type UlipProduct,
  type PartARow,
  type PartBRow,
  API_BASE_URL,
} from '../api';
import {
  SELF_MANAGED_FUNDS,
  calculateAge,
  deriveUlipPptYears,
  deriveUlipValues,
  getUlipPptYearOptions,
  getUlipPtOptions,
  shouldShowFundOption,
  validateUlip,
  onInvestmentStrategyChange,
  type EwealthRoyaleForm,
  type InvestmentStrategy,
  type PremiumFrequency,
  type PptType,
  type Gender,
  MODAL_FACTORS,
} from '../utils/biRules';

// ---------------------------------------------------------------------------
// Abbreviations displayed in the UI:
//   AP  = Annualized Premium      LA  = Loyalty Addition
//   SA  = Sum Assured             WB  = Wealth Booster
//   PT  = Policy Term             RoC = Return of Charges
//   PPT = Premium Payment Term    ARB = Additional Risk Benefit
//   FV  = Fund Value              PAC = Policy Administration Charge
//   MC  = Mortality Charge        FMC = Fund Management Charge
//   DB  = Death Benefit           SV  = Surrender Value
// ---------------------------------------------------------------------------

const INR = (v: number) =>
  v.toLocaleString('en-IN', { maximumFractionDigits: 0 });

const INPUT_CLS = `w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
  focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]`;

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export default function UlipIllustration() {
  const [products, setProducts]   = useState<UlipProduct[]>([]);
  const [form, setForm]           = useState<EwealthRoyaleForm>({
    product: 'EWEALTH_ROYALE',
    option: 'Platinum',
    isProposerDifferent: false,
    lifeAssuredName: '',
    proposerName: '',
    lifeAssuredDob: '',
    proposerDob: '',
    lifeAssuredGender: null,
    proposerGender: null,
    lifeAssuredAge: null,
    proposerAge: null,
    premium: 50000,
    premiumFrequency: 'Yearly',
    annualisedPremium: null,
    pptType: 'Limited',
    pptYears: 10,
    pt: 20,
    policyEffectiveDate: '',
    investmentStrategy: 'Self-Managed Investment Strategy',
    fundOption: null,
    standardAgeProof: true,
    salesChannel: 'Corporate Agency',
    staffPolicy: false,
  });
  const [result,  setResult]  = useState<UlipCalculationResult | null>(null);
  const [error,   setError]   = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showDisclaimer, setShowDisclaimer] = useState(false);
  const [activeTab, setActiveTab] = useState<'partA' | 'partB4' | 'partB8'>('partA');

  const derived = useMemo(() => deriveUlipValues(form), [form]);
  const lifeAssuredAge = calculateAge(form.lifeAssuredDob ?? null);
  const policyholderAge = calculateAge(form.proposerDob ?? null);

  // Load ULIP products
  useEffect(() => {
    getUlipProducts()
      .then(r => {
        setProducts(r.data);
        if (r.data.length > 0) setForm(f => ({ ...f, product: 'EWEALTH_ROYALE' }));
      })
      .catch(() => {/* silently fall back to default code */});
  }, []);

  const set = <K extends keyof EwealthRoyaleForm>(
    key: K,
    value: EwealthRoyaleForm[K],
  ) => setForm(prev => ({ ...prev, [key]: value }));

  const handleStrategyChange = (value: InvestmentStrategy) => {
    setForm(prev => onInvestmentStrategyChange(value, prev));
  };

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const nextForm: EwealthRoyaleForm = {
        ...form,
        lifeAssuredAge,
        proposerAge: policyholderAge,
        annualisedPremium: derived.annualisedPremium,
        sumAssured: derived.sumAssured,
      };

      const validationErrors = validateUlip(nextForm);
      if (validationErrors.length > 0) {
        setError(validationErrors.join('; '));
        return;
      }

      const entryAge = lifeAssuredAge ?? 0;
      const payload: UlipCalculationRequest = {
        customerName: nextForm.lifeAssuredName,
        policyholderName: nextForm.isProposerDifferent ? nextForm.proposerName ?? '' : nextForm.lifeAssuredName,
        lifeAssuredSameAsPolicyholder: !nextForm.isProposerDifferent,
        productCode: 'EWEALTH-ROYALE',
        option: (nextForm.option as any) ?? 'Platinum',
        gender: (nextForm.lifeAssuredGender ?? 'Male') as any,
        dateOfBirth: nextForm.lifeAssuredDob ?? '',
        entryAge,
        policyholderDateOfBirth: nextForm.isProposerDifferent ? nextForm.proposerDob ?? '' : nextForm.lifeAssuredDob ?? '',
        policyholderAge: policyholderAge ?? entryAge,
        policyholderGender: nextForm.isProposerDifferent ? (nextForm.proposerGender ?? 'Male') as any : (nextForm.lifeAssuredGender ?? 'Male') as any,
        typeOfPpt: (nextForm.pptType === 'Regular' ? 'Regular' : nextForm.pptType) as any,
        policyTerm: nextForm.pt ?? 0,
        ppt: nextForm.pptYears ?? 0,
        annualizedPremium: derived.annualisedPremium ?? 0,
        sumAssured: derived.sumAssured ?? 0,
        premiumFrequency: (nextForm.premiumFrequency === 'Half-Yearly' ? 'Half Yearly' : nextForm.premiumFrequency) as any,
        policyEffectiveDate: nextForm.policyEffectiveDate ?? '',
        fundOption: shouldShowFundOption(nextForm.investmentStrategy) ? nextForm.fundOption ?? '' : '',
        investmentStrategy: (nextForm.investmentStrategy ?? 'Self-Managed Investment Strategy') as any,
        riskPreference: undefined,
        fundAllocations: shouldShowFundOption(nextForm.investmentStrategy)
          ? [{ fundType: nextForm.fundOption ?? '', allocationPercent: 100 }]
          : [],
        distributionChannel: nextForm.salesChannel ?? '',
        isStaffFamily: nextForm.staffPolicy ?? false,
        standardAgeProofLA: nextForm.standardAgeProof ?? true,
        standardAgeProofPH: nextForm.standardAgeProof ?? true,
      };
      const resp = await runUlipCalculation(payload);
      setResult(resp.data);
      setActiveTab('partA');
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: unknown }; message?: string })?.response?.data
        ?? (e as { message?: string })?.message;
      setError(
        typeof msg === 'string' && msg
          ? msg
          : 'Unable to generate ULIP illustration. Please verify all fields and try again.',
      );
    } finally {
      setLoading(false);
    }
  };

  const isSelfManaged = shouldShowFundOption(form.investmentStrategy);

  // ---- PDF download ----
  const handleDownload = () => {
    if (!result) return;
    const url = `${API_BASE_URL}/api/ulip/pdf/${encodeURIComponent(result.policyNumber)}`;
    window.open(url, '_blank');
  };

  // ---------------------------------------------------------------------------
  // Sub-components for Part A / Part B tables
  // ---------------------------------------------------------------------------
  const PartATable = ({ rows }: { rows: PartARow[] }) => (
    <div className="overflow-x-auto">
      <p className="text-xs text-slate-500 px-2 py-1">
        * Other Charges = Policy Administration Charge + Fund Management Charge (PAC ₹100/month, FMC 0.1118%/month)
      </p>
      <table className="w-full text-xs border-collapse">
        <thead>
          <tr className="bg-[#004282] text-white">
            <th className="px-3 py-2 text-center" rowSpan={2}>Year</th>
            <th className="px-3 py-2 text-right" rowSpan={2}>Annualized Premium</th>
            <th className="px-3 py-2 text-center bg-indigo-700" colSpan={7}>At 4% p.a. Gross Return</th>
            <th className="px-3 py-2 text-center bg-emerald-700" colSpan={7}>At 8% p.a. Gross Return</th>
          </tr>
          <tr className="bg-[#003070] text-white text-xs">
            {['Mortality Charges','ARB Charges','Other Charges','GST','Fund at End of Year','Surrender Value','Death Benefit'].map(h => (
              <th key={h+'4'} className="px-2 py-1.5 text-right bg-indigo-800">{h}</th>
            ))}
            {['Mortality Charges','ARB Charges','Other Charges','GST','Fund at End of Year','Surrender Value','Death Benefit'].map(h => (
              <th key={h+'8'} className="px-2 py-1.5 text-right bg-emerald-800">{h}</th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {rows.map(r => (
            <tr key={r.year} className={`hover:bg-slate-50 ${r.year % 5 === 0 ? 'font-semibold bg-yellow-50/40' : ''}`}>
              <td className="px-3 py-1.5 text-center font-medium">{r.year}</td>
              <td className="px-3 py-1.5 text-right">{r.annualizedPremium ? `₹${INR(r.annualizedPremium)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right bg-indigo-50/30 text-rose-600">{r.mortalityCharges4 ? `₹${INR(r.mortalityCharges4)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right bg-indigo-50/30">{r.arbCharges4 ? `₹${INR(r.arbCharges4)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right bg-indigo-50/30 text-amber-700">₹{INR(r.otherCharges4)}</td>
              <td className="px-2 py-1.5 text-right bg-indigo-50/30">{r.gst4 ? `₹${INR(r.gst4)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right bg-indigo-50/30 font-medium">₹{INR(r.fundAtEndOfYear4)}</td>
              <td className="px-2 py-1.5 text-right bg-indigo-50/30">₹{INR(r.surrenderValue4)}</td>
              <td className="px-2 py-1.5 text-right bg-indigo-50/30">₹{INR(r.deathBenefit4)}</td>
              <td className="px-2 py-1.5 text-right bg-emerald-50/30 text-rose-600">{r.mortalityCharges8 ? `₹${INR(r.mortalityCharges8)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right bg-emerald-50/30">{r.arbCharges8 ? `₹${INR(r.arbCharges8)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right bg-emerald-50/30 text-amber-700">₹{INR(r.otherCharges8)}</td>
              <td className="px-2 py-1.5 text-right bg-emerald-50/30">{r.gst8 ? `₹${INR(r.gst8)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right bg-emerald-50/30 font-semibold text-emerald-800">₹{INR(r.fundAtEndOfYear8)}</td>
              <td className="px-2 py-1.5 text-right bg-emerald-50/30">₹{INR(r.surrenderValue8)}</td>
              <td className="px-2 py-1.5 text-right bg-emerald-50/30">₹{INR(r.deathBenefit8)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  const PartBTable = ({ rows, rateLabel }: { rows: PartBRow[]; rateLabel: string }) => (
    <div className="overflow-x-auto">
      <p className="text-xs text-slate-500 px-2 py-1">Gross Return: {rateLabel} | LA = Loyalty Addition | WB = Wealth Booster | RoC = Return of Policy Admin / Mortality Charges</p>
      <table className="w-full text-xs border-collapse">
        <thead>
          <tr className="bg-[#004282] text-white">
            <th className="px-2 py-2 text-center">Year</th>
            <th className="px-2 py-2 text-right">Annualized Premium</th>
            <th className="px-2 py-2 text-right">Premium Allocation Charge</th>
            <th className="px-2 py-2 text-right">Premium after PAC</th>
            <th className="px-2 py-2 text-right">Mortality Charges</th>
            <th className="px-2 py-2 text-right">ARB Charges</th>
            <th className="px-2 py-2 text-right">Policy Admin Charges</th>
            <th className="px-2 py-2 text-right">Fund before FMC</th>
            <th className="px-2 py-2 text-right">Fund Management Charge</th>
            <th className="px-2 py-2 text-right text-yellow-200">Loyalty Addition</th>
            <th className="px-2 py-2 text-right text-yellow-200">Wealth Booster</th>
            <th className="px-2 py-2 text-right text-yellow-200">Return of Charges</th>
            <th className="px-2 py-2 text-right">Fund at End of Year</th>
            <th className="px-2 py-2 text-right">Surrender Value</th>
            <th className="px-2 py-2 text-right">Death Benefit</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {rows.map(r => (
            <tr key={r.year} className={`hover:bg-slate-50 ${r.year % 5 === 0 ? 'font-semibold bg-yellow-50/40' : ''}`}>
              <td className="px-2 py-1.5 text-center font-medium">{r.year}</td>
              <td className="px-2 py-1.5 text-right">{r.annualizedPremium ? `₹${INR(r.annualizedPremium)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right">{r.premiumAllocationCharge ? `₹${INR(r.premiumAllocationCharge)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right">{r.annualizedPremiumAfterPac ? `₹${INR(r.annualizedPremiumAfterPac)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right text-rose-600">{r.mortalityCharges ? `₹${INR(r.mortalityCharges)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right">{r.arbCharges ? `₹${INR(r.arbCharges)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right text-amber-700">{r.policyAdministrationCharges ? `₹${INR(r.policyAdministrationCharges)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right">₹{INR(r.fundBeforeFmc)}</td>
              <td className="px-2 py-1.5 text-right text-amber-600">₹{INR(r.fundManagementCharge)}</td>
              <td className="px-2 py-1.5 text-right text-green-700">{r.loyaltyAddition ? `₹${INR(r.loyaltyAddition)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right text-green-700">{r.wealthBooster ? `₹${INR(r.wealthBooster)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right text-green-700">{r.returnOfCharges ? `₹${INR(r.returnOfCharges)}` : '—'}</td>
              <td className="px-2 py-1.5 text-right font-semibold">₹{INR(r.fundAtEndOfYear)}</td>
              <td className="px-2 py-1.5 text-right">₹{INR(r.surrenderValue)}</td>
              <td className="px-2 py-1.5 text-right">₹{INR(r.deathBenefit)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------
  return (
    <div className="space-y-8">
      {/* Heading */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          ULIP — Benefit Illustration
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          SUD Life e-Wealth Royale — IRDAI-compliant illustration at&nbsp;
          <span className="font-semibold text-slate-700">4%</span> and&nbsp;
          <span className="font-semibold text-slate-700">8%</span> assumed returns.
        </p>
      </div>

      {/* ── 2-Section input layout ── */}
      <div className="grid lg:grid-cols-2 gap-6">
        {/* Section 1 — Plan Parameters */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <div className="flex items-center gap-2 mb-2">
            <Settings2 size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Plan Parameters</h3>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Product</label>
              <select value={form.product}
                onChange={e => set('product', e.target.value as EwealthRoyaleForm['product'])}
                className={INPUT_CLS}>
                {products.length > 0
                  ? products.map(p => <option key={p.code} value="EWEALTH_ROYALE">{p.name}</option>)
                  : <option value="EWEALTH_ROYALE">SUD Life e-Wealth Royale</option>}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Option</label>
              <select value={form.option ?? 'Platinum'} onChange={e => set('option', e.target.value)} className={INPUT_CLS}>
                <option value="Platinum">Platinum</option>
                <option value="Platinum Plus">Platinum Plus</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Policy Effective Date</label>
              <input
                type="date"
                value={form.policyEffectiveDate ?? ''}
                onChange={e => set('policyEffectiveDate', e.target.value || null)}
                className={INPUT_CLS}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Premium Frequency</label>
              <select
                value={form.premiumFrequency ?? 'Yearly'}
                onChange={e => set('premiumFrequency', e.target.value as PremiumFrequency)}
                className={INPUT_CLS}>
                {(['Yearly', 'Half-Yearly', 'Quarterly', 'Monthly'] as PremiumFrequency[]).map(f => (
                  <option key={f} value={f}>{f}</option>
                ))}
              </select>
              {form.premiumFrequency && (
                <p className="text-[11px] text-slate-400 mt-1">Modal factor: {MODAL_FACTORS[form.premiumFrequency]}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Premium (Installment)</label>
              <input
                type="number"
                value={form.premium ?? ''}
                onChange={e => set('premium', e.target.value ? Number(e.target.value) : null)}
                className={INPUT_CLS}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Annualised Premium (Derived)</label>
              <input
                type="number"
                value={(derived.annualisedPremium ?? 0).toFixed(0)}
                readOnly
                className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Sum Assured (Derived)</label>
              <input
                type="number"
                value={(derived.sumAssured ?? 0).toFixed(0)}
                readOnly
                className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">PPT Type</label>
              <select
                value={form.pptType ?? 'Limited'}
                onChange={e => {
                  const type = e.target.value as PptType;
                  const nextPptYears = deriveUlipPptYears(type, form.pt, getUlipPptYearOptions(type)[0]);
                  const nextPt = getUlipPtOptions(type, nextPptYears)[0] ?? form.pt ?? 10;
                  setForm(prev => ({ ...prev, pptType: type, pptYears: nextPptYears, pt: nextPt }));
                }}
                className={INPUT_CLS}>
                <option value="Single">Single</option>
                <option value="Limited">Limited</option>
                <option value="Regular">Regular</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Premium Payment Term (PPT)</label>
              <select
                value={form.pptYears ?? ''}
                onChange={e => {
                  const val = e.target.value ? Number(e.target.value) : null;
                  const nextPtOptions = getUlipPtOptions(form.pptType, val);
                  const nextPt = nextPtOptions.includes(form.pt ?? 0) ? form.pt : (nextPtOptions[0] ?? null);
                  setForm(prev => ({ ...prev, pptYears: val, pt: nextPt }));
                }}
                disabled={form.pptType === 'Regular'}
                className={INPUT_CLS}>
                <option value="">Select PPT</option>
                {getUlipPptYearOptions(form.pptType).map(p => (
                  <option key={p} value={p}>{p}</option>
                ))}
                {form.pptType === 'Regular' && form.pt && <option value={form.pt}>{form.pt}</option>}
              </select>
              {form.pptType === 'Regular' && (
                <p className="text-[11px] text-slate-400 mt-1">Regular pay aligns PPT with Policy Term.</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Policy Term (PT)</label>
              <select
                value={form.pt ?? ''}
                onChange={e => {
                  const nextPt = e.target.value ? Number(e.target.value) : null;
                  const nextPptYears = deriveUlipPptYears(form.pptType, nextPt, form.pptYears);
                  setForm(prev => ({ ...prev, pt: nextPt, pptYears: nextPptYears }));
                }}
                className={INPUT_CLS}>
                <option value="">Select PT</option>
                {getUlipPtOptions(form.pptType, form.pptYears).map(p => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Investment Strategy</label>
              <select
                value={form.investmentStrategy ?? 'Self-Managed Investment Strategy'}
                onChange={e => handleStrategyChange(e.target.value as InvestmentStrategy)}
                className={INPUT_CLS}>
                <option value="Self-Managed Investment Strategy">Self-Managed Investment Strategy</option>
                <option value="Age-Based Strategy">Age-Based Strategy</option>
                <option value="System Managed">System Managed</option>
              </select>
            </div>

            {isSelfManaged && (
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Fund Option</label>
                <select
                  value={form.fundOption ?? ''}
                  onChange={e => set('fundOption', e.target.value || null)}
                  className={INPUT_CLS}>
                  <option value="">Select Fund</option>
                  {SELF_MANAGED_FUNDS.map(f => (
                    <option key={f} value={f}>{f}</option>
                  ))}
                </select>
              </div>
            )}

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Sales Channel</label>
              <select
                value={form.salesChannel ?? ''}
                onChange={e => set('salesChannel', e.target.value as any)}
                className={INPUT_CLS}>
                {['Corporate Agency', 'Agency', 'Broker', 'Direct Marketing', 'Online', 'Insurance Marketing Firm', 'Web Aggregator'].map(ch => (
                  <option key={ch} value={ch}>{ch}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Staff Policy</label>
              <select value={form.staffPolicy ? 'Yes' : 'No'}
                onChange={e => set('staffPolicy', e.target.value === 'Yes')}
                className={INPUT_CLS}>
                <option value="No">No</option>
                <option value="Yes">Yes</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Standard Age Proof</label>
              <select value={form.standardAgeProof ? 'Yes' : 'No'}
                onChange={e => set('standardAgeProof', e.target.value === 'Yes')}
                className={INPUT_CLS}>
                <option value="Yes">Yes</option>
                <option value="No">No</option>
              </select>
            </div>
          </div>

          <button onClick={handleCalculate}
            disabled={loading}
            className="w-full py-3 px-6 rounded-xl bg-[#004282] text-white font-semibold text-sm
                       hover:bg-[#003570] disabled:opacity-50 disabled:cursor-not-allowed
                       transition-colors shadow-md flex items-center justify-center gap-2">
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

        {/* Section 2 — Life Assured & Proposer */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <div className="flex items-center gap-2 mb-2">
            <User size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Life Assured & Proposer</h3>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Life Assured Name</label>
              <input
                type="text"
                value={form.lifeAssuredName}
                onChange={e => set('lifeAssuredName', e.target.value)}
                placeholder="Life Assured Name"
                className={INPUT_CLS}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Life Assured Gender</label>
              <select
                value={form.lifeAssuredGender ?? ''}
                onChange={e => set('lifeAssuredGender', e.target.value as Gender)}
                className={INPUT_CLS}>
                <option value="">Select</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Transgender">Transgender</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Life Assured DOB</label>
              <input
                type="date"
                value={form.lifeAssuredDob ?? ''}
                onChange={e => set('lifeAssuredDob', e.target.value || '')}
                className={INPUT_CLS}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Life Assured Age (auto)</label>
              <input type="number" value={lifeAssuredAge ?? ''} readOnly className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`} />
            </div>
          </div>

          <div className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              id="laSameUlip"
              checked={!form.isProposerDifferent}
              onChange={e => set('isProposerDifferent', !e.target.checked)}
              className="accent-[#004282]"
            />
            <label htmlFor="laSameUlip">Proposer is same as Life Assured</label>
          </div>

          {form.isProposerDifferent && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Proposer Name</label>
                <input
                  type="text"
                  value={form.proposerName ?? ''}
                  onChange={e => set('proposerName', e.target.value)}
                  className={INPUT_CLS}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Proposer Gender</label>
                <select
                  value={form.proposerGender ?? ''}
                  onChange={e => set('proposerGender', e.target.value as Gender)}
                  className={INPUT_CLS}>
                  <option value="">Select</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Transgender">Transgender</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Proposer DOB</label>
                <input
                  type="date"
                  value={form.proposerDob ?? ''}
                  onChange={e => set('proposerDob', e.target.value || '')}
                  className={INPUT_CLS}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Proposer Age (auto)</label>
                <input type="number" value={policyholderAge ?? ''} readOnly className={`${INPUT_CLS} bg-slate-50 cursor-not-allowed`} />
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ── Results ── */}
      {loading && (
        <div className="flex items-center justify-center py-24">
          <span className="inline-block w-10 h-10 border-2 border-[#007bff]/20 border-t-[#007bff] rounded-full animate-spin" />
        </div>
      )}

      {!loading && result && (
        <>
          {/* Summary cards */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              { label: 'Annualized Premium (AP)', value: `₹${INR(result.annualizedPremium)}` },
              { label: 'Sum Assured (SA)',         value: `₹${INR(result.sumAssured)}` },
              { label: 'Maturity Benefit @ 4%',   value: `₹${INR(result.maturityBenefit4)}`, highlight: true },
              { label: 'Maturity Benefit @ 8%',   value: `₹${INR(result.maturityBenefit8)}`, highlight: true },
            ].map(card => (
              <div key={card.label}
                className={`bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-4
                  ${card.highlight ? 'border-l-4 border-[#007bff]' : ''}`}>
                <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">{card.label}</p>
                <p className="text-xl font-extrabold text-[#004282]">{card.value}</p>
              </div>
            ))}
          </div>

          {/* Policy at a glance */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6">
            <h3 className="text-base font-bold text-[#004282] mb-4 flex items-center gap-2">
              <TrendingUp size={18} className="text-[#007bff]" />
              Policy At a Glance — SUD Life e-Wealth Royale
            </h3>
            <div className="grid sm:grid-cols-2 gap-2 text-sm">
              {[
                ['Customer Name',        result.customerName],
                ['Product',              result.productName],
                ['Option',               result.option],
                ['Gender',               result.gender],
                ['Entry Age',            `${result.entryAge} yrs`],
                ['Maturity Age',         `${result.maturityAge} yrs`],
                ['Policy Term (PT)',     `${result.policyTerm} yrs`],
                ['PPT',                  `${result.ppt} yrs`],
                ['Premium Frequency',    result.premiumFrequency],
                ['Net Yield @ 4%',       `${result.netYield4}%`],
                ['Net Yield @ 8%',       `${result.netYield8}%`],
                ['GST Rate',             '0%'],
              ].map(([k, v]) => (
                <div key={k} className="flex gap-2">
                  <span className="text-slate-400 w-44 shrink-0">{k}</span>
                  <span className="font-medium text-slate-700">{v}</span>
                </div>
              ))}
            </div>

            <button onClick={handleDownload}
              className="mt-4 flex items-center gap-2 px-4 py-2 rounded-full border border-[#004282]
                         text-[#004282] text-sm font-semibold hover:bg-blue-50 transition-colors">
              <FileDown size={14} />
              Download Illustration (HTML / Print to PDF)
            </button>
          </div>

          {/* Part A / Part B tabs */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
            <div className="px-6 pt-4 border-b border-slate-100 flex gap-1">
              {([
                { key: 'partA',  label: 'Part A — Summary (4% & 8%)' },
                { key: 'partB8', label: 'Part B @ 8%' },
                { key: 'partB4', label: 'Part B @ 4%' },
              ] as const).map(tab => (
                <button key={tab.key} onClick={() => setActiveTab(tab.key)}
                  className={`px-4 py-2.5 text-xs font-semibold rounded-t-lg border-b-2 transition-colors
                    ${activeTab === tab.key
                      ? 'text-[#004282] border-[#007bff] bg-blue-50/60'
                      : 'text-slate-400 border-transparent hover:text-slate-600 hover:border-slate-300'}`}>
                  {tab.label}
                </button>
              ))}
            </div>

            <div className="p-2">
              {activeTab === 'partA'  && <PartATable rows={result.partARows ?? []} />}
              {activeTab === 'partB8' && <PartBTable rows={result.partBRows8 ?? []} rateLabel="8% p.a." />}
              {activeTab === 'partB4' && <PartBTable rows={result.partBRows4 ?? []} rateLabel="4% p.a." />}
            </div>
          </div>

          {/* IRDAI Disclaimer */}
          <div className="bg-amber-50 border border-amber-200 rounded-xl p-5">
            <button onClick={() => setShowDisclaimer(d => !d)}
              className="flex items-center gap-2 text-amber-700 font-semibold text-sm w-full text-left">
              <Info size={16} />
              IRDAI Disclaimer
              {showDisclaimer ? <ChevronUp size={14} className="ml-auto" /> : <ChevronDown size={14} className="ml-auto" />}
            </button>
            {showDisclaimer && (
              <p className="mt-3 text-xs text-amber-800 leading-relaxed">{result.irdaiDisclaimer}</p>
            )}
          </div>
        </>
      )}

      {/* Empty state */}
      {!loading && !result && !error && (
        <div className="flex flex-col items-center justify-center py-32 text-slate-300 space-y-4">
          <TrendingUp size={56} />
          <p className="text-base font-semibold text-slate-400">
            Fill in the policy details and click <span className="text-[#004282]">Generate Illustration</span>
          </p>
        </div>
      )}
    </div>
  );
}

import { useState, useEffect } from 'react';
import { TrendingUp, AlertCircle, Info, ChevronDown, ChevronUp, FileDown, User, Settings2 } from 'lucide-react';
import {
  getUlipProducts,
  runUlipCalculation,
  type UlipCalculationRequest,
  type UlipCalculationResult,
  type UlipProduct,
  type PartARow,
  type PartBRow,
} from '../api';

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
  const [form, setForm]           = useState<UlipCalculationRequest>({
    policyNumber:        '',
    customerName:        '',
    policyholderName:    '',
    productCode:         'EWEALTH-ROYALE',
    option:              'Platinum',
    gender:              'Male',
    dateOfBirth:         '',
    entryAge:            37,
    policyholderDateOfBirth: '',
    policyholderAge:     37,
    policyholderGender:  'Male',
    typeOfPpt:           'Limited',
    policyTerm:          20,
    ppt:                 10,
    annualizedPremium:   24000,
    sumAssured:          240000,
    premiumFrequency:    'Yearly',
    policyEffectiveDate: '',
    fundOption:          '',
    investmentStrategy:  'Self-Managed Investment Strategy',
    riskPreference:      undefined,
    fundAllocations:     [{ fundType: 'SUD Life Nifty Alpha 50 Index Fund', allocationPercent: 100 }],
    distributionChannel: 'Corporate Agency',
    isStaffFamily:       false,
    ageRiskCommencement: 37,
    standardAgeProofLA:  true,
    standardAgeProofPH:  true,
    emrClassLifeAssured: 'Standard',
    emrClassPolicyholder:'Standard',
    flatExtraLifeAssured: 0,
    flatExtraPolicyholder: 0,
    keralaFloodCess:     false,
  });
  const [result,  setResult]  = useState<UlipCalculationResult | null>(null);
  const [error,   setError]   = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showDisclaimer, setShowDisclaimer] = useState(false);
  const [activeTab, setActiveTab] = useState<'partA' | 'partB4' | 'partB8'>('partA');

  // Load ULIP products
  useEffect(() => {
    getUlipProducts()
      .then(r => {
        setProducts(r.data);
        if (r.data.length > 0) setForm(f => ({ ...f, productCode: r.data[0].code }));
      })
      .catch(() => {/* silently fall back to default code */});
  }, []);

  const set = <K extends keyof UlipCalculationRequest>(
    key: K,
    value: UlipCalculationRequest[K],
  ) => setForm(prev => ({ ...prev, [key]: value }));

  const handleStrategyChange = (value: UlipCalculationRequest['investmentStrategy']) => {
    setForm(prev => {
      const nextRiskPref = value === 'Age-based Investment Strategy'
        ? (prev.riskPreference ?? 'Aggressive')
        : undefined;
      const nextAllocations = value === 'Age-based Investment Strategy'
        ? []
        : (prev.fundAllocations.length ? prev.fundAllocations : [{ fundType: '', allocationPercent: 0 }]);

      return {
        ...prev,
        investmentStrategy: value,
        riskPreference: nextRiskPref,
        fundAllocations: nextAllocations,
      };
    });
  };

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const resp = await runUlipCalculation(form);
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

  // ---- fund allocation helpers ----
  const isSelfManaged = form.investmentStrategy === 'Self-Managed Investment Strategy' || form.investmentStrategy === 'Self-Managed';
  const totalAlloc = form.fundAllocations.reduce((s, f) => s + f.allocationPercent, 0);
  const allocError = isSelfManaged && Math.abs(totalAlloc - 100) > 0.01;
  const allocationStepError = isSelfManaged && form.fundAllocations.some(f => f.allocationPercent % 5 !== 0);

  const updateAlloc = (idx: number, field: 'fundType' | 'allocationPercent', val: string | number) => {
    setForm(prev => {
      const updated = prev.fundAllocations.map((f, i) =>
        i === idx ? { ...f, [field]: val } : f,
      );
      return { ...prev, fundAllocations: updated };
    });
  };

  const addAlloc = () =>
    setForm(prev => ({
      ...prev,
      fundAllocations: [...prev.fundAllocations, { fundType: '', allocationPercent: 0 }],
    }));

  const removeAlloc = (idx: number) =>
    setForm(prev => ({
      ...prev,
      fundAllocations: prev.fundAllocations.filter((_, i) => i !== idx),
    }));

  // ---- PDF download ----
  const handleDownload = () => {
    if (!result) return;
    const url = `${import.meta.env.VITE_API_URL || 'http://localhost:5000'}/api/ulip/pdf/${encodeURIComponent(result.policyNumber)}`;
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
            <th className="px-3 py-2 text-right" rowSpan={2}>AP</th>
            <th className="px-3 py-2 text-center bg-indigo-700" colSpan={7}>At 4% p.a. Gross Return</th>
            <th className="px-3 py-2 text-center bg-emerald-700" colSpan={7}>At 8% p.a. Gross Return</th>
          </tr>
          <tr className="bg-[#003070] text-white text-xs">
            {['MC','ARB','Other*','GST','Fund','SV','DB'].map(h => (
              <th key={h+'4'} className="px-2 py-1.5 text-right bg-indigo-800">{h}</th>
            ))}
            {['MC','ARB','Other*','GST','Fund','SV','DB'].map(h => (
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
            <th className="px-2 py-2 text-right">AP</th>
            <th className="px-2 py-2 text-right">PAC%</th>
            <th className="px-2 py-2 text-right">AP−PAC</th>
            <th className="px-2 py-2 text-right">MC</th>
            <th className="px-2 py-2 text-right">ARB</th>
            <th className="px-2 py-2 text-right">Admin</th>
            <th className="px-2 py-2 text-right">Fund<br/>Bef.FMC</th>
            <th className="px-2 py-2 text-right">FMC</th>
            <th className="px-2 py-2 text-right text-yellow-200">LA</th>
            <th className="px-2 py-2 text-right text-yellow-200">WB</th>
            <th className="px-2 py-2 text-right text-yellow-200">RoC</th>
            <th className="px-2 py-2 text-right">Fund End</th>
            <th className="px-2 py-2 text-right">SV</th>
            <th className="px-2 py-2 text-right">DB</th>
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
        {/* Section 1 — Policyholder Details */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <div className="flex items-center gap-2 mb-2">
            <User size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Policyholder Details</h3>
          </div>

          {/* Product */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Product</label>
            <select value={form.productCode} onChange={e => set('productCode', e.target.value)} className={INPUT_CLS}>
              {products.length > 0
                ? products.map(p => <option key={p.code} value={p.code}>{p.name}</option>)
                : <option value="EWEALTH-ROYALE">SUD Life e-Wealth Royale</option>}
            </select>
          </div>

          {/* Option */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Option</label>
            <select value={form.option} onChange={e => set('option', e.target.value as 'Platinum' | 'Platinum Plus')} className={INPUT_CLS}>
              <option value="Platinum">Platinum</option>
              <option value="Platinum Plus">Platinum Plus</option>
            </select>
          </div>

          {/* Policy Number */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Policy Number</label>
            <input type="text" value={form.policyNumber} onChange={e => set('policyNumber', e.target.value)}
              placeholder="e.g. UL-2026-0001" className={INPUT_CLS} />
          </div>

          {/* Customer Name */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Name (Life Assured)</label>
            <input type="text" value={form.customerName} onChange={e => set('customerName', e.target.value)}
              placeholder="Full name" className={INPUT_CLS} />
          </div>

          {/* Gender */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Gender</label>
            <select value={form.gender} onChange={e => set('gender', e.target.value as 'Male' | 'Female')} className={INPUT_CLS}>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
            </select>
          </div>

          {/* Date of Birth */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Date of Birth (Life Assured)</label>
            <input type="date" value={form.dateOfBirth} onChange={e => set('dateOfBirth', e.target.value)} className={INPUT_CLS} />
          </div>

          {/* Entry Age */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Age (Life Assured)</label>
            <input type="number" value={form.entryAge} onChange={e => set('entryAge', parseInt(e.target.value) || 0)} className={INPUT_CLS} />
          </div>

          {/* Standard Age Proof LA */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Standard Age Proof (Life Assured)</label>
            <select value={form.standardAgeProofLA ? 'Yes' : 'No'}
              onChange={e => set('standardAgeProofLA', e.target.value === 'Yes')} className={INPUT_CLS}>
              <option value="Yes">Yes</option>
              <option value="No">No</option>
            </select>
          </div>

          {/* Standard Age Proof PH */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Standard Age Proof (Policyholder)</label>
            <select value={form.standardAgeProofPH ? 'Yes' : 'No'}
              onChange={e => set('standardAgeProofPH', e.target.value === 'Yes')} className={INPUT_CLS}>
              <option value="Yes">Yes</option>
              <option value="No">No</option>
            </select>
          </div>

          {/* Policy Effective Date */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Policy Effective Date <span className="text-slate-400 text-xs">(optional)</span></label>
            <input type="date" value={form.policyEffectiveDate ?? ''} onChange={e => set('policyEffectiveDate', e.target.value || undefined)} className={INPUT_CLS} />
          </div>

          {/* Policyholder Name */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Policyholder Name</label>
            <input type="text" value={form.policyholderName ?? ''} onChange={e => set('policyholderName', e.target.value)}
              placeholder="Full name (if different from Life Assured)" className={INPUT_CLS} />
          </div>

          {/* Policyholder Gender */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Gender (Policyholder)</label>
            <select value={form.policyholderGender ?? 'Male'} onChange={e => set('policyholderGender', e.target.value as 'Male' | 'Female')} className={INPUT_CLS}>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
            </select>
          </div>

          {/* Policyholder DOB */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Date of Birth (Policyholder)</label>
            <input type="date" value={form.policyholderDateOfBirth ?? ''} onChange={e => set('policyholderDateOfBirth', e.target.value || undefined)} className={INPUT_CLS} />
          </div>

          {/* Policyholder Age */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Age (Policyholder)</label>
            <input type="number" value={form.policyholderAge ?? 0} onChange={e => set('policyholderAge', parseInt(e.target.value) || 0)} className={INPUT_CLS} />
          </div>
        </div>

        {/* Section 2 — Plan Parameters */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
          <div className="flex items-center gap-2 mb-2">
            <Settings2 size={16} className="text-[#004282]" />
            <h3 className="text-sm font-bold text-[#004282] uppercase tracking-wider">Plan Parameters</h3>
          </div>

          <div className="grid grid-cols-2 gap-3">
            {/* Policy Term */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Policy Term (PT) — years</label>
              <input type="number" value={form.policyTerm} onChange={e => set('policyTerm', parseInt(e.target.value) || 0)} className={INPUT_CLS} />
            </div>

            {/* PPT */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Premium Payment Term (PPT)</label>
              <input type="number" value={form.ppt} onChange={e => set('ppt', parseInt(e.target.value) || 0)} className={INPUT_CLS} />
            </div>
          </div>

          {/* Annualized Premium */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Annualized Premium (AP) ₹</label>
            <input type="number" value={form.annualizedPremium} onChange={e => set('annualizedPremium', parseFloat(e.target.value) || 0)} className={INPUT_CLS} />
          </div>

          {/* Sum Assured */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Sum Assured (SA) ₹</label>
            <input type="number" value={form.sumAssured} onChange={e => set('sumAssured', parseFloat(e.target.value) || 0)} className={INPUT_CLS} />
          </div>

          {/* Premium Frequency */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Premium Frequency</label>
            <select value={form.premiumFrequency}
              onChange={e => set('premiumFrequency', e.target.value as UlipCalculationRequest['premiumFrequency'])} className={INPUT_CLS}>
              <option value="Yearly">Yearly</option>
              <option value="Half Yearly">Half Yearly</option>
              <option value="Quarterly">Quarterly</option>
              <option value="Monthly">Monthly</option>
            </select>
          </div>

          {/* Distribution Channel */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Distribution Channel</label>
            <select value={form.distributionChannel ?? ''} onChange={e => set('distributionChannel', e.target.value)} className={INPUT_CLS}>
              <option value="Corporate Agency">Corporate Agency</option>
              <option value="Agency">Agency</option>
              <option value="Broker">Broker</option>
              <option value="Direct Marketing">Direct Marketing</option>
              <option value="Online">Online</option>
              <option value="Insurance Marketing Firm">Insurance Marketing Firm</option>
            </select>
          </div>

          {/* Staff/Family */}
          <div className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" id="staffFamily" checked={form.isStaffFamily ?? false}
              onChange={e => set('isStaffFamily', e.target.checked)}
              className="accent-[#004282]" />
            <label htmlFor="staffFamily">Staff / Family Policy</label>
          </div>

          {/* Type of PPT */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Type of PPT</label>
            <select value={form.typeOfPpt ?? 'Limited'} onChange={e => set('typeOfPpt', e.target.value as 'Limited' | 'Till_Maturity')} className={INPUT_CLS}>
              <option value="Limited">Limited</option>
              <option value="Till_Maturity">Till Maturity</option>
            </select>
          </div>

          {/* Investment Strategy */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Investment Strategy</label>
            <select
              value={form.investmentStrategy ?? 'Self-Managed Investment Strategy'}
              onChange={e => handleStrategyChange(e.target.value as UlipCalculationRequest['investmentStrategy'])}
              className={INPUT_CLS}>
              <option value="Self-Managed Investment Strategy">Self-Managed Investment Strategy</option>
              <option value="Age-based Investment Strategy">Age-based Investment Strategy</option>
            </select>
          </div>

          {/* Risk Preference */}
          {form.investmentStrategy === 'Age-based Investment Strategy' && (
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Risk Preference</label>
              <select
                value={form.riskPreference ?? 'Aggressive'}
                onChange={e => set('riskPreference', e.target.value as 'Aggressive' | 'Conservative')}
                className={INPUT_CLS}>
                <option value="Aggressive">Aggressive</option>
                <option value="Conservative">Conservative</option>
              </select>
            </div>
          )}

          {/* Age at Risk Commencement */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Age at Risk Commencement</label>
            <input type="number" value={form.ageRiskCommencement ?? 0} onChange={e => set('ageRiskCommencement', parseInt(e.target.value) || 0)} className={INPUT_CLS} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            {/* EMR Class LA */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">EMR Class (Life Assured)</label>
              <select value={form.emrClassLifeAssured ?? 'Standard'} onChange={e => set('emrClassLifeAssured', e.target.value)} className={INPUT_CLS}>
                <option value="Standard">Standard</option>
                {[1,2,3,4,5,6,7,8,9].map(n => <option key={n} value={String(n)}>{n}</option>)}
              </select>
            </div>

            {/* EMR Class PH */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">EMR Class (Policyholder)</label>
              <select value={form.emrClassPolicyholder ?? 'Standard'} onChange={e => set('emrClassPolicyholder', e.target.value)} className={INPUT_CLS}>
                <option value="Standard">Standard</option>
                {[1,2,3,4,5,6,7,8,9].map(n => <option key={n} value={String(n)}>{n}</option>)}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            {/* Flat Extra LA */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Flat Extra (LA) ₹/1000 SAR</label>
              <input type="number" value={form.flatExtraLifeAssured ?? 0} onChange={e => set('flatExtraLifeAssured', parseFloat(e.target.value) || 0)} className={INPUT_CLS} />
            </div>

            {/* Flat Extra PH */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Flat Extra (PH) ₹/1000 SAR</label>
              <input type="number" value={form.flatExtraPolicyholder ?? 0} onChange={e => set('flatExtraPolicyholder', parseFloat(e.target.value) || 0)} className={INPUT_CLS} />
            </div>
          </div>

          {/* Kerala Flood Cess */}
          <div className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" id="keralaFloodCess" checked={form.keralaFloodCess ?? false}
              onChange={e => set('keralaFloodCess', e.target.checked)}
              className="accent-[#004282]" />
            <label htmlFor="keralaFloodCess">Kerala Flood Cess (applicable only for State of Kerala)</label>
          </div>

          {/* Fund Allocation */}
          {isSelfManaged && (
            <div className="pt-3 border-t border-slate-100 space-y-3">
              <h4 className="text-xs font-bold text-[#004282] uppercase tracking-wider">Fund Allocation</h4>

              {form.fundAllocations.map((alloc, idx) => (
                <div key={idx} className="flex gap-2 items-end">
                  <div className="flex-1">
                    <label className="block text-xs font-medium text-slate-500 mb-1">Fund</label>
                    <input type="text" value={alloc.fundType} onChange={e => updateAlloc(idx, 'fundType', e.target.value)}
                      placeholder="Fund name" className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-[#007bff]" />
                  </div>
                  <div className="w-16">
                    <label className="block text-xs font-medium text-slate-500 mb-1">%</label>
                    <input type="number" value={alloc.allocationPercent}
                      onChange={e => updateAlloc(idx, 'allocationPercent', parseFloat(e.target.value) || 0)}
                      min={0} max={100} className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-[#007bff]" />
                  </div>
                  {form.fundAllocations.length > 1 && (
                    <button onClick={() => removeAlloc(idx)} className="text-red-400 hover:text-red-600 text-xs pb-1" aria-label="Remove">✕</button>
                  )}
                </div>
              ))}

              <div className="flex items-center justify-between">
                <button onClick={addAlloc} className="text-xs text-[#007bff] hover:underline">+ Add Fund</button>
                <span className={`text-xs font-semibold ${allocError ? 'text-red-500' : 'text-green-600'}`}>Total: {totalAlloc}%</span>
              </div>
              {allocError && <p className="text-xs text-red-500">Fund allocations must sum to exactly 100%.</p>}
              {allocationStepError && <p className="text-xs text-red-500">Each fund allocation must be in multiples of 5%.</p>}
            </div>
          )}

          {/* Calculate button */}
          <button onClick={handleCalculate}
            disabled={loading || (isSelfManaged && (allocError || allocationStepError)) || !form.policyNumber}
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
                ['Policy Number',        result.policyNumber],
                ['Customer Name',        result.customerName],
                ['Product',              result.productName],
                ['Option',               result.option],
                ['Gender',               result.gender],
                ['Entry Age',            `${result.entryAge} yrs`],
                ['Maturity Age',         `${result.maturityAge} yrs`],
                ['Policy Term (PT)',     `${result.policyTerm} yrs`],
                ['PPT',                  `${result.ppt} yrs`],
                ['Premium Frequency',    result.premiumFrequency],
                ['Premium Installment',  `₹${INR(result.premiumInstallment)}`],
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

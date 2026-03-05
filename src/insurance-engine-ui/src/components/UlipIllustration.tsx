import { useState, useEffect } from 'react';
import { TrendingUp, AlertCircle, Info, ChevronDown, ChevronUp, FileDown } from 'lucide-react';
import {
  getUlipProducts,
  runUlipCalculation,
  type UlipCalculationRequest,
  type UlipCalculationResult,
  type UlipProduct,
} from '../api';

// ---------------------------------------------------------------------------
// Abbreviations displayed in the UI:
//   AP  = Annualized Premium
//   SA  = Sum Assured
//   PT  = Policy Term
//   PPT = Premium Payment Term
//   FV  = Fund Value
//   MC  = Mortality Charge
//   PC  = Policy Charge
//   DB  = Death Benefit
// ---------------------------------------------------------------------------

const INR = (v: number) =>
  v.toLocaleString('en-IN', { maximumFractionDigits: 0 });

const INR2 = (v: number) =>
  v.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export default function UlipIllustration() {
  const [products, setProducts]   = useState<UlipProduct[]>([]);
  const [form, setForm]           = useState<UlipCalculationRequest>({
    policyNumber:     '',
    customerName:     '',
    productCode:      'EWEALTH-ROYALE',
    gender:           'Male',
    dateOfBirth:      '',
    entryAge:         35,
    policyTerm:       10,
    ppt:              10,
    annualizedPremium: 100000,
    sumAssured:       1000000,
    premiumFrequency: 'Yearly',
    fundAllocations:  [{ fundType: 'Equity Growth Fund', allocationPercent: 100 }],
  });
  const [result,  setResult]  = useState<UlipCalculationResult | null>(null);
  const [error,   setError]   = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showDisclaimer, setShowDisclaimer] = useState(false);

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

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const resp = await runUlipCalculation(form);
      setResult(resp.data);
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
  const totalAlloc = form.fundAllocations.reduce((s, f) => s + f.allocationPercent, 0);
  const allocError = Math.abs(totalAlloc - 100) > 0.01;

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
          Unit Linked Insurance Plan — IRDAI-compliant illustration at&nbsp;
          <span className="font-semibold text-slate-700">4%</span> and&nbsp;
          <span className="font-semibold text-slate-700">8%</span> assumed returns.
        </p>
      </div>

      <div className="grid lg:grid-cols-4 gap-8">
        {/* ---------------------------------------------------------------- */}
        {/* Input panel                                                       */}
        {/* ---------------------------------------------------------------- */}
        <div className="lg:col-span-1 space-y-6">
          {/* Policy Inputs */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">
              Policy Inputs
            </h3>

            {/* Product */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Product</label>
              <select
                value={form.productCode}
                onChange={e => set('productCode', e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              >
                {products.length > 0
                  ? products.map(p => (
                      <option key={p.code} value={p.code}>{p.name}</option>
                    ))
                  : <option value="EWEALTH-ROYALE">e-Wealth Royale</option>
                }
              </select>
            </div>

            {/* Policy Number */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Policy Number</label>
              <input
                type="text"
                value={form.policyNumber}
                onChange={e => set('policyNumber', e.target.value)}
                placeholder="e.g. UL-2024-0001"
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* Customer Name */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Customer Name</label>
              <input
                type="text"
                value={form.customerName}
                onChange={e => set('customerName', e.target.value)}
                placeholder="Full name"
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* Gender */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Gender</label>
              <select
                value={form.gender}
                onChange={e => set('gender', e.target.value as 'Male' | 'Female')}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              >
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
            </div>

            {/* Date of Birth */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Date of Birth</label>
              <input
                type="date"
                value={form.dateOfBirth}
                onChange={e => set('dateOfBirth', e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* Entry Age */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Entry Age</label>
              <input
                type="number"
                value={form.entryAge}
                onChange={e => set('entryAge', parseInt(e.target.value) || 0)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* Policy Term (PT) */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Policy Term (PT) — years</label>
              <input
                type="number"
                value={form.policyTerm}
                onChange={e => set('policyTerm', parseInt(e.target.value) || 0)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* PPT */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Premium Payment Term (PPT)</label>
              <input
                type="number"
                value={form.ppt}
                onChange={e => set('ppt', parseInt(e.target.value) || 0)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* Annual Premium (AP) */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Annualized Premium (AP) ₹</label>
              <input
                type="number"
                value={form.annualizedPremium}
                onChange={e => set('annualizedPremium', parseFloat(e.target.value) || 0)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* Sum Assured (SA) */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Sum Assured (SA) ₹</label>
              <input
                type="number"
                value={form.sumAssured}
                onChange={e => set('sumAssured', parseFloat(e.target.value) || 0)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              />
            </div>

            {/* Premium Frequency */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Premium Frequency</label>
              <select
                value={form.premiumFrequency}
                onChange={e => set('premiumFrequency', e.target.value as UlipCalculationRequest['premiumFrequency'])}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff]"
              >
                <option value="Yearly">Yearly</option>
                <option value="HalfYearly">Half-Yearly</option>
                <option value="Quarterly">Quarterly</option>
                <option value="Monthly">Monthly</option>
              </select>
            </div>
          </div>

          {/* Fund Inputs */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-4">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">
              Fund Allocation
            </h3>

            {form.fundAllocations.map((alloc, idx) => (
              <div key={idx} className="flex gap-2 items-end">
                <div className="flex-1">
                  <label className="block text-xs font-medium text-slate-500 mb-1">Fund</label>
                  <input
                    type="text"
                    value={alloc.fundType}
                    onChange={e => updateAlloc(idx, 'fundType', e.target.value)}
                    placeholder="Fund name"
                    className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-xs
                               focus:outline-none focus:ring-2 focus:ring-[#007bff]"
                  />
                </div>
                <div className="w-20">
                  <label className="block text-xs font-medium text-slate-500 mb-1">%</label>
                  <input
                    type="number"
                    value={alloc.allocationPercent}
                    onChange={e => updateAlloc(idx, 'allocationPercent', parseFloat(e.target.value) || 0)}
                    min={0}
                    max={100}
                    className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-xs
                               focus:outline-none focus:ring-2 focus:ring-[#007bff]"
                  />
                </div>
                {form.fundAllocations.length > 1 && (
                  <button
                    onClick={() => removeAlloc(idx)}
                    className="text-red-400 hover:text-red-600 text-xs pb-1"
                    aria-label="Remove fund"
                  >✕</button>
                )}
              </div>
            ))}

            <div className="flex items-center justify-between">
              <button
                onClick={addAlloc}
                className="text-xs text-[#007bff] hover:underline"
              >+ Add Fund</button>
              <span className={`text-xs font-semibold ${allocError ? 'text-red-500' : 'text-green-600'}`}>
                Total: {totalAlloc}%
              </span>
            </div>
            {allocError && (
              <p className="text-xs text-red-500">Fund allocations must sum to exactly 100%.</p>
            )}
          </div>

          {/* Calculate button */}
          <button
            onClick={handleCalculate}
            disabled={loading || allocError || !form.policyNumber}
            className="w-full py-3 px-6 rounded-xl bg-[#004282] text-white font-semibold text-sm
                       hover:bg-[#003570] disabled:opacity-50 disabled:cursor-not-allowed
                       transition-colors shadow-md"
          >
            {loading ? 'Calculating…' : 'Generate Illustration'}
          </button>
        </div>

        {/* ---------------------------------------------------------------- */}
        {/* Results panel                                                     */}
        {/* ---------------------------------------------------------------- */}
        <div className="lg:col-span-3 space-y-6">
          {/* Error */}
          {error && (
            <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700">
              <AlertCircle size={16} className="mt-0.5 flex-shrink-0 text-[#d32f2f]" />
              {error}
            </div>
          )}

          {/* Loading skeleton */}
          {loading && (
            <div className="flex items-center justify-center py-24">
              <span className="inline-block w-10 h-10 border-2 border-[#007bff]/20 border-t-[#007bff]
                               rounded-full animate-spin" />
            </div>
          )}

          {/* Results */}
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
                    <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                      {card.label}
                    </p>
                    <p className="text-xl font-extrabold text-[#004282]">{card.value}</p>
                  </div>
                ))}
              </div>

              {/* Policy at a glance */}
              <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6">
                <h3 className="text-base font-bold text-[#004282] mb-4 flex items-center gap-2">
                  <TrendingUp size={18} className="text-[#007bff]" />
                  Policy At a Glance
                </h3>
                <div className="grid sm:grid-cols-2 gap-2 text-sm">
                  {[
                    ['Policy Number',          result.policyNumber],
                    ['Customer Name',           result.customerName],
                    ['Product',                 result.productName],
                    ['Gender',                  result.gender],
                    ['Entry Age',               `${result.entryAge} yrs`],
                    ['Policy Term (PT)',         `${result.policyTerm} yrs`],
                    ['PPT',                      `${result.ppt} yrs`],
                    ['Premium Frequency',        result.premiumFrequency],
                  ].map(([k, v]) => (
                    <div key={k} className="flex gap-2">
                      <span className="text-slate-400 w-44 shrink-0">{k}</span>
                      <span className="font-medium text-slate-700">{v}</span>
                    </div>
                  ))}
                </div>

                {/* Download button */}
                <button
                  onClick={handleDownload}
                  className="mt-4 flex items-center gap-2 px-4 py-2 rounded-full border border-[#004282]
                             text-[#004282] text-sm font-semibold hover:bg-blue-50 transition-colors"
                >
                  <FileDown size={14} />
                  Download Illustration (HTML)
                </button>
              </div>

              {/* Yearly table */}
              <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100">
                  <h3 className="text-base font-bold text-[#004282]">
                    Benefit Illustration Table
                    <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
                  </h3>
                  <p className="text-xs text-slate-400 mt-1">
                    AP = Annualized Premium · MC = Mortality Charge · PC = Policy Charge ·
                    FV = Fund Value · DB = Death Benefit
                  </p>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                        <th className="px-4 py-3 text-center">Year</th>
                        <th className="px-4 py-3 text-center">Age</th>
                        <th className="px-4 py-3 text-right">Annual Premium (AP)</th>
                        <th className="px-4 py-3 text-right">Premium Invested</th>
                        <th className="px-4 py-3 text-right">MC</th>
                        <th className="px-4 py-3 text-right">PC</th>
                        {/* 4% scenario */}
                        <th className="px-4 py-3 text-right bg-indigo-50/50">FV @ 4%</th>
                        <th className="px-4 py-3 text-right bg-indigo-50/50">DB @ 4%</th>
                        {/* 8% scenario */}
                        <th className="px-4 py-3 text-right bg-emerald-50/50">FV @ 8%</th>
                        <th className="px-4 py-3 text-right bg-emerald-50/50">DB @ 8%</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {result.yearlyTable.map(row => (
                        <tr key={row.year} className="hover:bg-slate-50 transition-colors text-slate-700">
                          <td className="px-4 py-2.5 text-center font-medium">{row.year}</td>
                          <td className="px-4 py-2.5 text-center text-slate-500">{row.age}</td>
                          <td className="px-4 py-2.5 text-right">₹{INR(row.annualPremium)}</td>
                          <td className="px-4 py-2.5 text-right">₹{INR(row.premiumInvested)}</td>
                          <td className="px-4 py-2.5 text-right text-rose-600">₹{INR2(row.mortalityCharge)}</td>
                          <td className="px-4 py-2.5 text-right text-amber-600">₹{INR(row.policyCharge)}</td>
                          <td className="px-4 py-2.5 text-right bg-indigo-50/30 font-medium">₹{INR(row.fundValue4)}</td>
                          <td className="px-4 py-2.5 text-right bg-indigo-50/30">₹{INR(row.deathBenefit4)}</td>
                          <td className="px-4 py-2.5 text-right bg-emerald-50/30 font-medium text-emerald-700">₹{INR(row.fundValue8)}</td>
                          <td className="px-4 py-2.5 text-right bg-emerald-50/30 text-emerald-700">₹{INR(row.deathBenefit8)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* IRDAI Disclaimer */}
              <div className="bg-amber-50 border border-amber-200 rounded-xl p-5">
                <button
                  onClick={() => setShowDisclaimer(d => !d)}
                  className="flex items-center gap-2 text-amber-700 font-semibold text-sm w-full text-left"
                >
                  <Info size={16} />
                  IRDAI Disclaimer
                  {showDisclaimer
                    ? <ChevronUp size={14} className="ml-auto" />
                    : <ChevronDown size={14} className="ml-auto" />
                  }
                </button>
                {showDisclaimer && (
                  <p className="mt-3 text-xs text-amber-800 leading-relaxed">
                    {result.irdaiDisclaimer}
                  </p>
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
      </div>
    </div>
  );
}

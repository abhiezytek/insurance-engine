import { useEffect, useMemo, useState } from 'react';
import { Search, BarChart3, AlertCircle, FileDown } from 'lucide-react';
import { downloadYpygPdf, downloadYpygUlipPdf, type YpygPdfResult } from '../utils/pdfExport';
import { api } from '../api';
import { DEFAULT_YPYG_PRODUCTS, loadYpygProductMap, type YpygProductMap } from '../config/products';
import { YPYG_RESULT_META } from '../config/resultMeta';
import type { ProductParameter } from '../api';
const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 2 });

// ─── Types ──────────────────────────────────────────────────────────────────

interface PolicyData {
  policyNumber: string;
  customerName: string;
  productType: string;
  productCode: string;
  productCategory: string;
  uin: string;
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
  gender: string;
  dateOfBirth: string;
  premiumFrequency: string;
  premiumStatus: string;
  dateOfCommencement: string;
  riskCommencementDate: string;
  pendingPremiums: number;
  survivalBenefitPaid: number;
  investmentStrategy: string;
}

interface YpygUlipRow {
  year: number;
  age: number;
  annualPremium: number;
  premiumInvested: number;
  mortalityCharge: number;
  policyCharge: number;
  fundValue4: number;
  deathBenefit4: number;
  surrenderValue4: number;
  fundValue8: number;
  deathBenefit8: number;
  surrenderValue8: number;
}

interface YpygResult {
  policyNumber: string;
  productCode: string;
  productCategory: string;
  productVersion?: string;
  factorVersion?: string;
  formulaVersion?: string;
  uin: string;
  customerName: string;
  gender?: string;
  policyStatus: string;
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
  // ULIP-specific
  fundValue4?: number;
  fundValue8?: number;
  maturityBenefit4?: number;
  maturityBenefit8?: number;
  ulipYearlyTable?: YpygUlipRow[];
  // Total Benefit Value fields
  calculationDate?: string;
  currentPolicyYear?: number;
  currentSurvivalBenefit: number;
  maturitySurvivalBenefit: number;
  currentMaturityBenefit: number;
  maturityMaturityBenefit: number;
  currentDeathBenefit: number;
  maturityDeathBenefit: number;
  // ULIP current-date values
  currentFundValue4?: number;
  currentFundValue8?: number;
  maturityFundValue4?: number;
  maturityFundValue8?: number;
  currentDeathBenefit4?: number;
  currentDeathBenefit8?: number;
  maturityDeathBenefit4?: number;
  maturityDeathBenefit8?: number;
  validationRules?: string[];
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
      const res = await api.get<PolicyData>(`/api/ypyg/policy/${encodeURIComponent(policyNumber.trim())}`);
      setPolicy(res.data);
    } catch (e: any) {
      setLookupError(e?.response?.data?.error || e?.message || 'Policy not found.');
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
      const res = await api.post<YpygResult>('/api/ypyg/calculate', {
        policyNumber: policy.policyNumber,
        productCode: policy.productCode,
        productCategory: policy.productCategory,
        uin: policy.uin,
        customerName: policy.customerName,
        gender: policy.gender,
        dateOfBirth: policy.dateOfBirth,
        premiumFrequency: policy.premiumFrequency,
        policyStatus: policy.policyStatus,
        investmentStrategy: policy.investmentStrategy,
        annualPremium: policy.annualPremium,
        policyTerm: policy.policyTerm,
        premiumPayingTerm: policy.premiumPayingTerm,
        premiumsPaid: policy.premiumsPaid,
        sumAssured: policy.sumAssured,
        entryAge: policy.entryAge,
        option: policy.option,
        channel: policy.channel,
        fundValue: policy.fundValue,
        riskCommencementDate: policy.riskCommencementDate || undefined,
      });
      setResult(res.data);
    } catch (e: any) {
      setCalcError(e?.response?.data?.error || e?.message || 'Calculation failed.');
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
                ['Category', policy.productCategory],
                ['UIN', policy.uin || 'N/A'],
                ['Annual Premium', `₹ ${INR(policy.annualPremium)}`],
                ['Policy Term', `${policy.policyTerm} yrs`],
                ['Premium Paying Term', `${policy.premiumPayingTerm} yrs`],
                ['Premiums Paid', `${policy.premiumsPaid} yrs`],
                ['Sum Assured', `₹ ${INR(policy.sumAssured)}`],
                ['Entry Age', `${policy.entryAge} yrs`],
                ['Gender', policy.gender],
                ['Premium Frequency', policy.premiumFrequency],
                ['Option', policy.option],
                ['Channel', policy.channel],
                ...(policy.productCategory === 'ULIP' ? [
                  ['Fund Value', `₹ ${INR(policy.fundValue)}`],
                  ['Investment Strategy', policy.investmentStrategy || 'N/A'],
                ] : []),
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
  productCategory: 'Traditional' as string,
  productVersion: '',
  annualPremium: 50000,
  policyTerm: 20,
  premiumPayingTerm: 10,
  premiumsPaid: 5,
  sumAssured: 500000,
  entryAge: 35,
  gender: 'Male' as string,
  option: 'Immediate',
  channel: 'Other',
  fundValue: 0,
  riskCommencementDate: '' as string,
  policyStatus: 'In-Force' as string,
  investmentStrategy: 'Self-Managed Investment Strategy' as string,
  premiumFrequency: 'Yearly' as string,
  customerName: '' as string,
  uin: '' as string,
};

function InputValueMode() {
  const [form, setForm] = useState(DEFAULT_INPUTS);
  const [productMap, setProductMap] = useState<YpygProductMap>(DEFAULT_YPYG_PRODUCTS);
  const [parameters, setParameters] = useState<ProductParameter[]>([]);
  const [paramValues, setParamValues] = useState<Record<string, string>>({});
  const productConfig = useMemo(
    () => productMap[form.productCategory] ?? DEFAULT_YPYG_PRODUCTS.Traditional,
    [form.productCategory, productMap],
  );

  useEffect(() => {
    loadYpygProductMap().then(setProductMap).catch(() => setProductMap(DEFAULT_YPYG_PRODUCTS));
  }, []);

  useEffect(() => {
    setForm(f => ({ ...f, productVersion: '' }));
    setParamValues({});
  }, [form.productCategory]);

  useEffect(() => {
    api.get<ProductParameter[]>('/api/admin/parameters', {
      params: { productCode: productConfig.code, version: form.productVersion || undefined },
    }).then(res => setParameters(res.data)).catch(() => setParameters([]));
  }, [productConfig.code, form.productVersion]);
  const [result, setResult] = useState<YpygResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const set = (k: keyof typeof DEFAULT_INPUTS, v: string | number) =>
    setForm(f => ({ ...f, [k]: v }));
  const isUlip = form.productCategory === 'ULIP';

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const { riskCommencementDate, productVersion, ...rest } = form;
      const payload = {
        ...rest,
        ...(riskCommencementDate ? { riskCommencementDate } : {}),
        productCode: productConfig.code,
        ...(Object.keys(paramValues).length ? { additionalParameters: paramValues } : {}),
        ...(productVersion ? { productVersion } : {}),
      };
      const res = await api.post<YpygResult>('/api/ypyg/calculate', payload);
      setResult(res.data);
    } catch (e: any) {
      setError(e?.response?.data?.error || e?.message || 'Calculation failed.');
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

          <Field label="Product Type">
            <select
              value={form.productCategory}
              onChange={e => set('productCategory', e.target.value)}
              className={INPUT_CLS}
            >
              {Object.entries(productMap).map(([key, cfg]) => (
                <option key={key} value={key}>
                  {cfg.displayName}
                </option>
              ))}
            </select>
          </Field>
          {productConfig.versions && productConfig.versions.length > 0 && (
            <Field label="Product Version">
              <select
                value={form.productVersion}
                onChange={e => set('productVersion', e.target.value)}
                className={INPUT_CLS}
              >
                <option value="">Latest</option>
                {productConfig.versions.map(v => (
                  <option key={v} value={v}>{v}</option>
                ))}
              </select>
            </Field>
          )}
          {parameters.length > 0 && (
            <div className="space-y-3 border border-slate-100 rounded-lg p-3 bg-slate-50/60">
              <p className="text-xs font-semibold text-slate-500 uppercase">Config-driven inputs</p>
              {parameters.map(p => (
                <Field key={p.id} label={`${p.name}${p.isRequired ? ' *' : ''}`}>
                  <input
                    type={p.dataType === 'number' ? 'number' : p.dataType === 'date' ? 'date' : 'text'}
                    value={paramValues[p.name] ?? ''}
                    onChange={e => setParamValues(v => ({ ...v, [p.name]: e.target.value }))}
                    className={INPUT_CLS}
                    placeholder={p.description}
                  />
                </Field>
              ))}
            </div>
          )}
          <Field label="Policy Number (optional)">
            <input type="text" value={form.policyNumber}
              onChange={e => set('policyNumber', e.target.value)}
              className={INPUT_CLS} placeholder="Optional" />
          </Field>
          <Field label="Customer Name (optional)">
            <input type="text" value={form.customerName}
              onChange={e => set('customerName', e.target.value)}
              className={INPUT_CLS} placeholder="Optional" />
          </Field>
          <Field label="UIN (optional)">
            <input type="text" value={form.uin}
              onChange={e => set('uin', e.target.value)}
              className={INPUT_CLS} placeholder={isUlip ? '142L079V03' : '142N066V02'} />
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
          <Field label="Gender">
            <select value={form.gender}
              onChange={e => set('gender', e.target.value)}
              className={INPUT_CLS}>
              <option>Male</option>
              <option>Female</option>
            </select>
          </Field>
          <Field label="Premium Frequency">
            <select value={form.premiumFrequency}
              onChange={e => set('premiumFrequency', e.target.value)}
              className={INPUT_CLS}>
              <option>Yearly</option>
              <option>Half Yearly</option>
              <option>Quarterly</option>
              <option>Monthly</option>
            </select>
          </Field>
          <Field label="Policy Status">
            <select value={form.policyStatus}
              onChange={e => set('policyStatus', e.target.value)}
              className={INPUT_CLS}>
              <option>In-Force</option>
              <option>Paid-Up</option>
              <option>Lapsed</option>
              <option>Revived</option>
              <option>Discontinued</option>
            </select>
          </Field>
          {isUlip ? (
            <>
              <Field label="Investment Strategy">
                <select value={form.investmentStrategy}
                  onChange={e => set('investmentStrategy', e.target.value)}
                  className={INPUT_CLS}>
                  <option>Self-Managed Investment Strategy</option>
                  <option>Age-based Investment Strategy</option>
                </select>
              </Field>
              <Field label="Fund Value (₹)">
                <input type="number" value={form.fundValue}
                  onChange={e => set('fundValue', +e.target.value)}
                  className={INPUT_CLS} />
              </Field>
               <Field label="Option">
                 <select value={form.option}
                   onChange={e => set('option', e.target.value)}
                   className={INPUT_CLS}>
                   {productConfig.options.map(o => <option key={o}>{o}</option>)}
                 </select>
               </Field>
             </>
           ) : (
             <>
               <Field label="Option">
                 <select value={form.option}
                   onChange={e => set('option', e.target.value)}
                   className={INPUT_CLS}>
                   {productConfig.options.map(o => <option key={o}>{o}</option>)}
                 </select>
               </Field>
               {productConfig.channels && (
                 <Field label="Channel">
                   <select value={form.channel}
                     onChange={e => set('channel', e.target.value)}
                     className={INPUT_CLS}>
                     {productConfig.channels.map(c => <option key={c}>{c}</option>)}
                   </select>
                 </Field>
               )}
             </>
           )}
          <Field label="Risk Commencement Date">
            <input type="date" value={form.riskCommencementDate}
              onChange={e => set('riskCommencementDate', e.target.value)}
              className={INPUT_CLS}
              title="Date when risk cover started (YPYG mode). Leave blank for pre-issuance." />
            <p className="mt-1 text-xs text-slate-400">Used to determine elapsed policy years</p>
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
  const isUlip = result.productCategory === 'ULIP';
  const resultMeta = YPYG_RESULT_META[isUlip ? 'ULIP' : 'Traditional'];
  const calcDate = result.calculationDate
    ? new Date(result.calculationDate).toLocaleDateString('en-IN', { day: '2-digit', month: '2-digit', year: 'numeric' })
    : new Date().toLocaleDateString('en-IN', { day: '2-digit', month: '2-digit', year: 'numeric' });

  return (
    <div className="space-y-6">
      {/* ── Total Benefit Value table ── */}
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b-2 border-[#00796b]">
          <h3 className="text-center text-lg font-extrabold text-[#004282] tracking-wide">Total Benefit Value</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-50 text-slate-500 uppercase tracking-wider text-xs">
                <th className="px-6 py-3 text-left font-semibold w-1/3"></th>
                <th className="px-6 py-3 text-right font-semibold w-1/3">As on Current Date</th>
                <th className="px-6 py-3 text-right font-semibold w-1/3">At Maturity</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {resultMeta.tbvRows.map(row => (
                <tr key={row.label} className="hover:bg-slate-50">
                  <td className="px-6 py-3 font-semibold text-slate-700">{row.label}</td>
                  <td className="px-6 py-3 text-right font-bold text-[#004282]">
                    ₹ {INR((result as any)[row.currentKey] ?? 0)}
                  </td>
                  <td className="px-6 py-3 text-right font-bold text-green-700">
                    ₹ {INR((result as any)[row.maturityKey] ?? 0)}
                  </td>
                </tr>
              ))}
              <tr className="bg-slate-50/50">
                <td className="px-6 py-3 font-semibold text-slate-700">Calculation Date</td>
                <td className="px-6 py-3 text-right font-semibold text-slate-600" colSpan={2}>{calcDate}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      {/* ── Additional info ── */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <InfoCard label="Policy Number" value={result.policyNumber || '—'} />
        <InfoCard label="Product Code" value={result.productCode || '—'} />
        <InfoCard label="Sum Assured on Death" value={`₹ ${INR(result.sumAssuredOnDeath)}`} />
        {isUlip ? (
          <>
            <InfoCard label="Current Policy Year" value={`Year ${result.currentPolicyYear ?? 1}`} />
            <InfoCard label="Policy Status" value={result.policyStatus || 'In-Force'} />
          </>
        ) : (
          <>
            <InfoCard label="Max Loan Amount" value={`₹ ${INR(result.maxLoanAmount)}`} />
            <InfoCard label="Policy Status" value={result.policyStatus || 'In-Force'} />
          </>
        )}
      </div>
      {result.validationRules && result.validationRules.length > 0 && (
        <div className="bg-white rounded-xl border border-slate-100 p-4 text-xs text-slate-600">
          <p className="font-semibold text-slate-700 mb-2">Validation Rules (preview)</p>
          <div className="flex flex-wrap gap-2">
            {result.validationRules.map(r => (
              <span key={r} className="px-2 py-1 bg-slate-100 rounded-full">{r}</span>
            ))}
          </div>
        </div>
      )}

      {/* Yearly table */}
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
          <h3 className="text-base font-bold text-[#004282]">
            {resultMeta.tableTitle}
            <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
          </h3>
          <button
            onClick={() => {
              const template = {
                title: resultMeta.tableTitle,
                subtitle: `Policy Number: ${result.policyNumber || 'N/A'}`,
                tbvRows: resultMeta.tbvRows.map(r => ({ ...r })),
                tableHeaders: [...resultMeta.yearlyHeaders],
              };
              if (isUlip && result.ulipYearlyTable) {
                downloadYpygUlipPdf({
                  policyNumber: result.policyNumber,
                  customerName: result.customerName || '',
                  productCode: result.productCode,
                  gender: result.gender,
                  entryAge: result.ulipYearlyTable.length > 0 ? result.ulipYearlyTable[0].age : 0,
                  policyTerm: result.policyTerm,
                  ppt: result.premiumPayingTerm,
                  annualizedPremium: result.annualPremium,
                  sumAssured: result.sumAssuredOnDeath,
                  maturityBenefit4: result.maturityBenefit4 ?? 0,
                  maturityBenefit8: result.maturityBenefit8 ?? 0,
                  currentFundValue4: result.currentFundValue4,
                  currentFundValue8: result.currentFundValue8,
                  maturityFundValue4: result.maturityFundValue4,
                  maturityFundValue8: result.maturityFundValue8,
                  currentDeathBenefit4: result.currentDeathBenefit4,
                  currentDeathBenefit8: result.currentDeathBenefit8,
                  maturityDeathBenefit4: result.maturityDeathBenefit4,
                  maturityDeathBenefit8: result.maturityDeathBenefit8,
                  calculationDate: result.calculationDate,
                  yearlyTable: result.ulipYearlyTable,
                }, template);
              } else {
                downloadYpygPdf(result as YpygPdfResult, result.policyNumber, template);
              }
            }}
            className="ml-auto flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold
                       bg-[#004282] text-white rounded-lg hover:bg-[#003370] transition"
          >
            <FileDown size={13} />
            Download PDF
          </button>
        </div>
        <div className="overflow-x-auto">
          {isUlip && result.ulipYearlyTable ? (
            <table className="w-full text-xs">
              <thead>
                <tr className="bg-blue-50/60 text-slate-500 uppercase tracking-wider">
                  {resultMeta.yearlyHeaders.map(h => (
                    <th key={h} className="px-3 py-3 text-right first:text-center">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {result.ulipYearlyTable.map(row => (
                  <tr key={row.year} className="hover:bg-slate-50 text-slate-700">
                    <td className="px-3 py-2 text-center font-semibold text-[#004282]">{row.year}</td>
                    <td className="px-3 py-2 text-right">{row.age}</td>
                    <td className="px-3 py-2 text-right">{INR(row.annualPremium)}</td>
                    <td className="px-3 py-2 text-right">{INR(row.premiumInvested)}</td>
                    <td className="px-3 py-2 text-right">{INR(row.mortalityCharge)}</td>
                    <td className="px-3 py-2 text-right">{INR(row.policyCharge)}</td>
                    <td className="px-3 py-2 text-right">{INR(row.fundValue4)}</td>
                    <td className="px-3 py-2 text-right text-[#d32f2f]">{INR(row.deathBenefit4)}</td>
                    <td className="px-3 py-2 text-right">{INR(row.surrenderValue4)}</td>
                    <td className="px-3 py-2 text-right">{INR(row.fundValue8)}</td>
                    <td className="px-3 py-2 text-right text-[#d32f2f]">{INR(row.deathBenefit8)}</td>
                    <td className="px-3 py-2 text-right">{INR(row.surrenderValue8)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <table className="w-full text-xs">
              <thead>
                <tr className="bg-blue-50/60 text-slate-500 uppercase tracking-wider">
                  {resultMeta.yearlyHeaders.map(h => (
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
          )}
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

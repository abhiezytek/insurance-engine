import { useEffect, useMemo, useState } from 'react';
import { AlertCircle, BarChart3, FileDown, RefreshCcw, Search } from 'lucide-react';
import { downloadYpygPdf, downloadYpygUlipPdf, type YpygPdfResult } from '../utils/pdfExport';
import { api } from '../api';
import { DEFAULT_YPYG_PRODUCTS, loadYpygProductMap, type YpygProductMap, type YpygProductMeta } from '../config/products';
import { YPYG_RESULT_META } from '../config/resultMeta';
import type { ProductParameter } from '../api';
import {
  applyPolicyPrefill,
  applyProductDefaults,
  buildDefaultForm,
  buildVisibleFields,
  computeSumAssured,
  deriveAnnualPremium,
  resolvePtOptions,
  type BuildContext,
  type PolicyLookupModel,
  type YpygFieldDefinition,
  type YpygFormState,
} from '../ypyg/fieldSchema';
import { MOCK_TRADITIONAL_POLICY, MOCK_ULIP_POLICY } from '../ypyg/mockPolicies';

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 2 });

export type YpygMode = 'policy-number' | 'input-value';

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

type FormFieldKey = keyof YpygFormState;

export default function YpygModule({ mode }: { mode: YpygMode }) {
  const [pageMode, setPageMode] = useState<YpygMode>(mode);
  const [productMap, setProductMap] = useState<YpygProductMap>(DEFAULT_YPYG_PRODUCTS);
  const [form, setForm] = useState<YpygFormState>(buildDefaultForm(DEFAULT_YPYG_PRODUCTS));
  const [policyLookup, setPolicyLookup] = useState<PolicyLookupModel | null>(null);
  const [parameters, setParameters] = useState<ProductParameter[]>([]);
  const [paramValues, setParamValues] = useState<Record<string, string>>({});
  const [result, setResult] = useState<YpygResult | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [calcError, setCalcError] = useState<string | null>(null);
  const [lookupLoading, setLookupLoading] = useState(false);
  const [calcLoading, setCalcLoading] = useState(false);

  const productConfig = useMemo<YpygProductMeta>(() => productMap[form.productKey] ?? DEFAULT_YPYG_PRODUCTS.Traditional, [form.productKey, productMap]);
  const ptOptions = useMemo(() => resolvePtOptions(productConfig, form.ppt), [form.ppt, productConfig]);
  const visibleFields = useMemo(
    () => buildVisibleFields({ form, product: productConfig, ptOptions }),
    [form, productConfig, ptOptions],
  );

  useEffect(() => {
    loadYpygProductMap().then(map => {
      setProductMap(map);
      setForm(f => applyProductDefaults(f, map[f.productKey] ?? DEFAULT_YPYG_PRODUCTS.Traditional));
    }).catch(() => {
      setProductMap(DEFAULT_YPYG_PRODUCTS);
      setForm(f => applyProductDefaults(f, DEFAULT_YPYG_PRODUCTS[f.productKey]));
    });
  }, []);

  useEffect(() => {
    const cfg = productMap[form.productKey] ?? productConfig;
    api.get<ProductParameter[]>('/api/admin/parameters', {
      params: { productCode: cfg.code, version: form.productVersion || undefined },
    }).then(res => setParameters(res.data)).catch(() => setParameters([]));
  }, [form.productKey, form.productVersion, productMap, productConfig.code]);

  const updateField = (key: FormFieldKey, value: string | number) => {
    setForm(current => {
      let next: YpygFormState = { ...current, [key]: value };

      // Product switching
      if (key === 'productKey') {
        const newProduct = productMap[value as keyof YpygProductMap] ?? productConfig;
        next = applyProductDefaults(
          { ...next, productVersion: '', policyYear: 1 },
          newProduct,
        );
      }

      // PPT changes drive PT options and policy year bounds
      if (key === 'ppt') {
        const newPtOptions = resolvePtOptions(productConfig, Number(value));
        next.pt = newPtOptions[0] ?? Number(value);
        next.policyYear = Math.min(next.policyYear, next.pt);
      }

      if (key === 'policyYear') {
        next.policyYear = Math.min(Number(value), next.pt || Number(value));
      }

      // Frequency / modal premium updates annual premium
      if (key === 'modalPremium' || key === 'premiumFrequency') {
        next.annualPremium = deriveAnnualPremium(
          Number(next.modalPremium),
          next.premiumFrequency,
          productConfig,
        );
      }

      // Recompute sum assured where formula exists
      next.sumAssured = computeSumAssured(next, productConfig);
      return next;
    });
  };

  const handlePolicyFetch = async () => {
    if (!form.policyNumber.trim()) return;
    setLookupLoading(true);
    setLookupError(null);
    try {
      const res = await api.get<PolicyLookupModel>(`/api/ypyg/policy/${encodeURIComponent(form.policyNumber.trim())}`);
      const filled = applyPolicyPrefill(res.data, form, productMap);
      setForm(filled);
      setPolicyLookup(res.data);
    } catch (e: any) {
      setLookupError(e?.response?.data?.error || e?.message || 'Policy not found.');
      setPolicyLookup(null);
    } finally {
      setLookupLoading(false);
    }
  };

  const handleCalculate = async () => {
    setCalcLoading(true);
    setCalcError(null);
    setResult(null);
    try {
      const payload = {
        policyNumber: form.policyNumber,
        productCode: productConfig.code,
        productCategory: productConfig.category,
        productVersion: form.productVersion || undefined,
        uin: form.uin,
        customerName: form.customerName,
        gender: form.gender,
        premiumFrequency: form.premiumFrequency,
        policyStatus: form.policyStatus,
        annualPremium: form.annualPremium,
        policyTerm: form.pt,
        premiumPayingTerm: form.ppt,
        premiumsPaid: form.policyYear,
        sumAssured: form.sumAssured,
        entryAge: form.age,
        option: form.productOption,
        channel: form.distributionChannel,
        fundValue: policyLookup?.fundValue ?? 0,
        riskCommencementDate: form.riskCommencementDate || undefined,
        investmentStrategy: policyLookup?.investmentStrategy,
        ...(Object.keys(paramValues).length ? { additionalParameters: paramValues } : {}),
      };
      const res = await api.post<YpygResult>('/api/ypyg/calculate', payload);
      setResult(res.data);
    } catch (e: any) {
      setCalcError(e?.response?.data?.error || e?.message || 'Calculation failed.');
    } finally {
      setCalcLoading(false);
    }
  };

  const handleUseMock = (mock: PolicyLookupModel) => {
    const filled = applyPolicyPrefill(mock, form, productMap);
    setForm(filled);
    setPolicyLookup(mock);
  };

  const resetForm = () => {
    setForm(buildDefaultForm(productMap));
    setPolicyLookup(null);
    setParamValues({});
    setResult(null);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h2 className="text-2xl font-bold text-[#004282]">
            YPYG — {pageMode === 'policy-number' ? 'Policy Driven' : 'Manual'} Mode
            <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
          </h2>
          <p className="mt-2 text-slate-500 text-sm max-w-2xl">
            Use policy number to auto-fill or manually enter the compact 2-column inputs.
            The left pane stays focused on inputs; the right pane mirrors the PDF-style summary.
          </p>
        </div>
        <div className="flex items-center gap-2 bg-white shadow-[0_4px_18px_rgba(0,0,0,0.08)] rounded-xl p-2">
          <ModeButton active={pageMode === 'policy-number'} onClick={() => setPageMode('policy-number')}>
            Policy Number
          </ModeButton>
          <ModeButton active={pageMode === 'input-value'} onClick={() => setPageMode('input-value')}>
            Manual
          </ModeButton>
        </div>
      </div>

      <div className="grid lg:grid-cols-2 gap-6 items-start">
        {/* Left pane */}
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgba(0,0,0,0.08)] p-6 space-y-5">
          {pageMode === 'policy-number' && (
            <div className="space-y-3">
              <div className="flex gap-3 flex-col sm:flex-row">
                <input
                  type="text"
                  value={form.policyNumber}
                  onChange={e => updateField('policyNumber', e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && handlePolicyFetch()}
                  placeholder="Enter Policy Number"
                  className="flex-1 px-4 py-2.5 rounded-lg border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-[#004282]"
                />
                <button
                  onClick={handlePolicyFetch}
                  disabled={lookupLoading}
                  className="flex items-center justify-center gap-2 px-4 py-2.5 bg-[#004282] text-white rounded-lg text-sm font-semibold hover:bg-[#003370] disabled:opacity-60 transition"
                >
                  <Search size={14} />
                  {lookupLoading ? 'Fetching…' : 'Fetch Policy'}
                </button>
              </div>
              {lookupError && <ErrorBanner message={lookupError} />}
              <div className="flex gap-2 text-xs text-slate-500">
                <button
                  className="flex items-center gap-1 px-3 py-1.5 rounded-full bg-slate-50 border border-slate-200 hover:border-slate-300"
                  onClick={() => handleUseMock(MOCK_TRADITIONAL_POLICY)}
                >
                  <RefreshCcw size={12} />
                  Prefill Traditional mock
                </button>
                <button
                  className="flex items-center gap-1 px-3 py-1.5 rounded-full bg-slate-50 border border-slate-200 hover:border-slate-300"
                  onClick={() => handleUseMock(MOCK_ULIP_POLICY)}
                >
                  <RefreshCcw size={12} />
                  Prefill ULIP mock
                </button>
              </div>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Field label="Product">
              <select
                value={form.productKey}
                onChange={e => updateField('productKey', e.target.value)}
                className={INPUT_CLS}
              >
                {(Object.keys(productMap) as Array<keyof YpygProductMap>).map(key => (
                  <option key={key} value={key}>
                    {productMap[key].displayName}
                  </option>
                ))}
              </select>
            </Field>

            {productConfig.versions && productConfig.versions.length > 0 && (
              <Field label="Product Version">
                <select
                  value={form.productVersion}
                  onChange={e => updateField('productVersion', e.target.value)}
                  className={INPUT_CLS}
                >
                  <option value="">Latest</option>
                  {productConfig.versions.map(v => (
                    <option key={v} value={v}>{v}</option>
                  ))}
                </select>
              </Field>
            )}

            {visibleFields.map(field => (
              <Field
                key={field.key}
                label={field.label}
                helper={field.helperText}
                span={field.columns === 2}
              >
                {renderInput(field, form, productConfig, updateField, ptOptions)}
              </Field>
            ))}

            {parameters.length > 0 && (
              <div className="md:col-span-2 border border-slate-100 rounded-lg p-3 bg-slate-50/60 space-y-3">
                <p className="text-xs font-semibold text-slate-500 uppercase">Config driven inputs</p>
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
          </div>

          <div className="flex flex-wrap gap-3 justify-end border-t border-slate-100 pt-3">
            <button
              onClick={resetForm}
              className="px-4 py-2 rounded-lg border border-slate-200 text-slate-700 text-sm hover:bg-slate-50"
            >
              Reset
            </button>
            <button
              onClick={handleCalculate}
              disabled={calcLoading}
              className="flex items-center gap-2 px-5 py-2 bg-[#004282] text-white rounded-lg text-sm font-semibold hover:bg-[#003370] disabled:opacity-60 transition"
            >
              <BarChart3 size={15} />
              {calcLoading ? 'Calculating…' : 'Calculate'}
            </button>
          </div>
          {calcError && <ErrorBanner message={calcError} />}
        </div>

        {/* Right pane */}
        <div className="space-y-4">
          <PolicyAtGlance
            product={productConfig}
            form={form}
            result={result}
            policy={policyLookup}
            onDownload={() => result && triggerDownload(result, productConfig)}
          />

          {result ? (
            <ResultSection result={result} productName={productConfig.displayName} />
          ) : (
            <div className="bg-white rounded-xl shadow-[0_8px_30px_rgba(0,0,0,0.08)] flex flex-col items-center justify-center h-64 text-slate-400 text-sm">
              <p className="font-semibold text-slate-600">Policy at a Glance</p>
              <p className="text-center mt-1">Outputs will appear here once you calculate.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function PolicyAtGlance({
  product,
  form,
  result,
  policy,
  onDownload,
}: {
  product: YpygProductMeta;
  form: YpygFormState;
  result: YpygResult | null;
  policy: PolicyLookupModel | null;
  onDownload: () => void;
}) {
  const isUlip = product.category === 'ULIP';
  const policyStatus = result?.policyStatus || form.policyStatus || 'In-Force';
  const premiumPaid = form.policyYear * form.annualPremium;
  const balancePayable = Math.max(form.ppt - form.policyYear, 0) * form.annualPremium;
  const valueTillDate = result?.currentMaturityBenefit ?? 0;
  const valueOnMaturity = result?.maturityMaturityBenefit ?? 0;
  const fundValueCurrent = result?.currentFundValue8 ?? result?.currentFundValue4 ?? result?.fundValue8 ?? 0;
  const fundValueProjected = result?.maturityFundValue8 ?? 0;
  const survivalBenefitInstalment = result?.yearlyTable?.[0]?.guaranteedIncome ?? 0;
  const summaryItems = [
    ['Policy Number', result?.policyNumber || form.policyNumber || '—'],
    ['Customer Name', result?.customerName || form.customerName || policy?.customerName || '—'],
    ['Product', product.displayName],
    ['UIN', form.uin || '—'],
    ['Premium Frequency', form.premiumFrequency],
    ['Premium Status', policy?.premiumStatus || 'Paid'],
    ['Policy Status', policyStatus],
  ];

  return (
    <div className="bg-white rounded-xl shadow-[0_8px_30px_rgba(0,0,0,0.08)] overflow-hidden">
      <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
        <div>
          <p className="text-xs text-slate-500 uppercase tracking-wider">Policy at a Glance</p>
          <h3 className="text-lg font-bold text-[#004282]">{product.displayName}</h3>
        </div>
        <div className="ml-auto flex items-center gap-2">
          <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-100">
            {product.category}
          </span>
          <button
            onClick={onDownload}
            disabled={!result}
            className="flex items-center gap-1.5 px-3 py-2 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003370] transition disabled:opacity-60"
          >
            <FileDown size={13} />
            Download PDF
          </button>
        </div>
      </div>

      <div className="p-6 space-y-4">
        <div className="grid sm:grid-cols-2 gap-3">
          {summaryItems.map(([label, value]) => (
            <SummaryCell key={label} label={label} value={value} />
          ))}
          <SummaryCell label="Premium Payment Term" value={`${result?.premiumPayingTerm ?? form.ppt} yrs`} />
          <SummaryCell label="Policy Term" value={`${result?.policyTerm ?? form.pt} yrs`} />
          <SummaryCell label="Risk Cover" value={`₹ ${INR(result?.sumAssuredOnDeath ?? form.sumAssured)}`} />
          {isUlip && (
            <SummaryCell label="Fund Option" value={form.fundOption || policy?.investmentStrategy || '—'} />
          )}
        </div>

        <div className="grid sm:grid-cols-2 gap-3">
          <ValueBlock title="Till Date" rows={[
            ['Value till date', `₹ ${INR(valueTillDate)}`],
            ['Premium Paid till date', `₹ ${INR(premiumPaid)}`],
            ['Balance Payable', `₹ ${INR(balancePayable)}`],
            ['Survival Benefit Paid till date', `₹ ${INR(policy?.survivalBenefitPaid ?? result?.currentSurvivalBenefit ?? 0)}`],
          ]} />
          <ValueBlock title="On Maturity" rows={[
            [isUlip ? 'Projected Fund Value @8%' : 'Value on Maturity', `₹ ${INR(isUlip ? fundValueProjected : valueOnMaturity)}`],
            [isUlip ? 'Fund Value as on date' : 'Revival Amount Due', `₹ ${INR(isUlip ? fundValueCurrent : policy?.pendingPremiums ?? 0)}`],
            ['Sum Assured / Risk Cover', `₹ ${INR(result?.sumAssuredOnDeath ?? form.sumAssured)}`],
            ['Survival Benefit Instalment', `₹ ${INR(survivalBenefitInstalment)}`],
          ]} />
        </div>

        <div className="text-[11px] text-slate-500 bg-slate-50 border border-slate-100 rounded-lg p-3">
          {isUlip
            ? 'Fund values are illustrative. Projected values @8% are not guaranteed. Please refer to the ULIP YPYG format for statutory wording.'
            : 'Values are indicative and aligned with the YPYG Traditional “Policy at a Glance” format.'}
        </div>
      </div>
    </div>
  );
}

function ResultSection({ result, productName }: { result: YpygResult; productName: string }) {
  const isUlip = result.productCategory === 'ULIP';
  const resultMeta = YPYG_RESULT_META[isUlip ? 'ULIP' : 'Traditional'];
  const readVal = (key: string) => {
    const raw = (result as unknown as Record<string, unknown>)[key];
    if (typeof raw === 'number') return raw;
    const parsed = Number(raw);
    return Number.isFinite(parsed) ? parsed : 0;
  };
  const calcDate = result.calculationDate
    ? new Date(result.calculationDate).toLocaleDateString('en-IN', { day: '2-digit', month: '2-digit', year: 'numeric' })
    : new Date().toLocaleDateString('en-IN', { day: '2-digit', month: '2-digit', year: 'numeric' });

  const template = {
    title: 'Policy at a Glance',
    subtitle: `${productName} — Policy Number: ${result.policyNumber || 'N/A'}`,
    tbvRows: resultMeta.tbvRows.map(r => ({ ...r })),
    tableHeaders: [...resultMeta.yearlyHeaders],
    sectionTitle: resultMeta.tableTitle,
  };

  return (
    <div className="space-y-5">
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgba(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b-2 border-[#00796b] flex items-center gap-3">
          <h3 className="text-base font-extrabold text-[#004282] tracking-wide">Total Benefit Value</h3>
          <button
            onClick={() => {
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
            className="ml-auto flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-[#004282] text-white rounded-lg hover:bg-[#003370] transition"
          >
            <FileDown size={13} />
            Download PDF
          </button>
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
                    ₹ {INR(readVal(row.currentKey))}
                  </td>
                  <td className="px-6 py-3 text-right font-bold text-green-700">
                    ₹ {INR(readVal(row.maturityKey))}
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

      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgba(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
          <h3 className="text-base font-bold text-[#004282]">
            {resultMeta.tableTitle}
            <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
          </h3>
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

function triggerDownload(result: YpygResult, product: YpygProductMeta) {
  const isUlip = result.productCategory === 'ULIP';
  const resultMeta = YPYG_RESULT_META[isUlip ? 'ULIP' : 'Traditional'];
  const template = {
    title: 'Policy at a Glance',
    subtitle: `${product.displayName} — Policy Number: ${result.policyNumber || 'N/A'}`,
    tbvRows: resultMeta.tbvRows.map(r => ({ ...r })),
    tableHeaders: [...resultMeta.yearlyHeaders],
    sectionTitle: resultMeta.tableTitle,
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
}

// ─── Helper components ───────────────────────────────────────────────────────

const INPUT_CLS =
  'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#007bff]';

function Field({ label, helper, span, children }: { label: string; helper?: string; span?: boolean; children: React.ReactNode }) {
  return (
    <div className={span ? 'md:col-span-2' : ''}>
      <label className="block text-xs font-semibold text-slate-600 mb-1">{label}</label>
      {children}
      {helper && <p className="mt-1 text-[11px] text-slate-400">{helper}</p>}
    </div>
  );
}

function SummaryCell({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="bg-slate-50 rounded-lg p-3 border border-slate-100">
      <p className="text-[11px] uppercase tracking-wide text-slate-500">{label}</p>
      <p className="text-sm font-semibold text-slate-800 mt-1 break-words">{value}</p>
    </div>
  );
}

function ValueBlock({ title, rows }: { title: string; rows: [string, string][] }) {
  return (
    <div className="rounded-lg border border-slate-100 p-3 bg-white shadow-[0_4px_12px_rgba(0,0,0,0.04)]">
      <p className="text-xs font-semibold text-slate-500 uppercase mb-2">{title}</p>
      <div className="space-y-1.5">
        {rows.map(([label, value]) => (
          <div key={label} className="flex items-center justify-between text-sm">
            <span className="text-slate-600">{label}</span>
            <span className="font-semibold text-[#004282]">{value}</span>
          </div>
        ))}
      </div>
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

function ModeButton({ active, children, onClick }: { active: boolean; children: React.ReactNode; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
        active
          ? 'bg-[#004282] text-white shadow-[0_4px_10px_rgba(0,0,0,0.15)]'
          : 'bg-slate-50 text-slate-600 border border-slate-200'
      }`}
    >
      {children}
    </button>
  );
}

function renderInput(
  field: YpygFieldDefinition,
  form: YpygFormState,
  product: YpygProductMeta,
  update: (key: FormFieldKey, value: string | number) => void,
  ptOptions: number[],
) {
  const value = form[field.key];
  const ctx: BuildContext = { product, form, ptOptions };
  const isReadOnly = typeof field.readOnly === 'function' ? field.readOnly(ctx) : field.readOnly;
  const resolvedOptions = typeof field.options === 'function' ? field.options(ctx) : field.options;
  switch (field.type) {
    case 'select':
      return (
        <select
          value={value as string}
          onChange={e => {
            const nextValue = field.key === 'ppt' || field.key === 'pt' ? Number(e.target.value) : e.target.value;
            update(field.key, nextValue);
          }}
          className={INPUT_CLS}
          disabled={isReadOnly}
        >
          {(resolvedOptions ?? []).map(opt => (
            <option key={opt} value={opt}>{opt}</option>
          ))}
        </select>
      );
    case 'date':
      return (
        <input
          type="date"
          value={(value as string) ?? ''}
          onChange={e => update(field.key, e.target.value)}
          className={INPUT_CLS}
          disabled={isReadOnly}
        />
      );
    case 'number':
      return (
        <input
          type="number"
          value={Number(value ?? 0)}
          onChange={e => update(field.key, Number(e.target.value))}
          className={INPUT_CLS}
          disabled={isReadOnly}
        />
      );
    default:
      return (
        <input
          type="text"
          value={(value as string) ?? ''}
          onChange={e => update(field.key, e.target.value)}
          className={INPUT_CLS}
          disabled={isReadOnly}
        />
      );
  }
}

import { useState, useEffect, useCallback, type JSX } from 'react';
import {
  ShieldCheck, Upload, Download, Search,
  FileSpreadsheet, AlertCircle, CheckCircle2,
  Filter, ChevronDown, FileJson, FileCog, ArrowRight,
} from 'lucide-react';
import { api } from '../api';
import { PAYOUT_ROUTE } from '../config/audit';

// ─── Constants & helpers ─────────────────────────────────────────────────────

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 2 });
const INPUT_CLS = 'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#007bff]';
const CARD_CLS = 'bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)]';
const BTN_PRIMARY = 'bg-[#004282] text-white rounded-xl font-semibold text-sm hover:bg-[#003370] disabled:opacity-60 transition flex items-center justify-center gap-2';

const fmtDate = (d: string) =>
  new Date(d).toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

// ─── Interfaces ──────────────────────────────────────────────────────────────

interface PayoutCase {
  id: number;
  policyNumber: string;
  productName: string;
  uin: string;
  payoutType: string;
  inputMode: string;
  batchId?: number | null;
  coreSystemAmount: number;
  precisionProAmount: number;
  variance: number;
  variancePct: number;
  status: string;
  remarks?: string | null;
  productVersion?: string | null;
  factorVersion?: string | null;
  formulaVersion?: string | null;
  calculationSource?: string | null;
  calculatedAt?: string;
  annualPremium?: number;
  policyTerm?: number;
  premiumPayingTerm?: number;
  createdBy: string;
  createdAt: string;
  workflowHistory: WorkflowStep[];
}

interface WorkflowStep {
  id: number;
  action: string;
  fromStatus?: string | null;
  toStatus: string;
  remarks?: string | null;
  performedBy: string;
  performedAt: string;
}

interface PayoutBatch {
  id: number;
  batchType: string;
  fileName?: string | null;
  payoutType: string;
  totalCount: number;
  processedCount: number;
  matchCount: number;
  mismatchCount: number;
  status: string;
  createdBy: string;
  createdAt: string;
}

interface PayoutDashboard {
  totalThisMonth: number;
  pendingCount: number;
  checkerApprovedCount: number;
  authorizedCount: number;
  rejectedCount: number;
  matchCount: number;
  mismatchCount: number;
  totalVariance: number;
}

// ─── Shared small components ─────────────────────────────────────────────────

function ErrBanner({ msg }: { msg: string }) {
  return (
    <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700">
      <AlertCircle size={14} className="mt-0.5 flex-shrink-0" />
      {msg}
    </div>
  );
}

function Spinner({ size = 16 }: { size?: number }) {
  return (
    <span
      className="inline-block border-2 border-[#007bff]/20 border-t-[#007bff] rounded-full animate-spin"
      style={{ width: size, height: size }}
    />
  );
}

function StatusBadge({ status }: { status: string }) {
  const s = status.toLowerCase();
  const cls =
    s === 'authorized' ? 'bg-green-100 text-green-700' :
    s === 'checkerapproved' ? 'bg-blue-100 text-blue-700' :
    s === 'rejected' || s === 'checkerrejected' ? 'bg-red-100 text-[#d32f2f]' :
    'bg-amber-100 text-amber-700';
  const labels: Record<string, string> = {
    pending: 'Pending',
    checkerapproved: 'Checker Approved',
    checkerrejected: 'Checker Rejected',
    authorized: 'Authorized',
    rejected: 'Rejected',
  };
  return <span className={`inline-block px-3 py-0.5 rounded-full text-xs font-bold ${cls}`}>{labels[s] ?? status}</span>;
}

function varianceRowColor(variancePct: number) {
  const abs = Math.abs(variancePct);
  if (abs <= 1) return 'bg-green-50/60';
  if (abs <= 5) return 'bg-amber-50/60';
  return 'bg-red-50/60';
}

// ─── Workflow Stepper ────────────────────────────────────────────────────────

const EXPECTED_FLOW = ['Submitted', 'L1 Approved', 'L2 Authorized', 'Complete'] as const;

function WorkflowStepper({ history, status }: { history: WorkflowStep[]; status: string }) {
  const getStepState = (stepLabel: string): 'completed' | 'current' | 'pending' => {
    const s = status.toLowerCase();
    switch (stepLabel) {
      case 'Submitted':
        return 'completed';
      case 'L1 Approved':
        if (history.some(h => h.action.toLowerCase().includes('checkerapprove') || h.toStatus === 'CheckerApproved'))
          return 'completed';
        if (s === 'pending') return 'current';
        return 'pending';
      case 'L2 Authorized':
        if (history.some(h => h.action.toLowerCase().includes('authorizerapprove') || h.toStatus === 'Authorized'))
          return 'completed';
        if (s === 'checkerapproved') return 'current';
        return 'pending';
      case 'Complete':
        if (s === 'authorized') return 'completed';
        return 'pending';
      default:
        return 'pending';
    }
  };

  const getStepMeta = (stepLabel: string) => {
    let matched: WorkflowStep | undefined;
    switch (stepLabel) {
      case 'Submitted':
        matched = history.find(h => h.action.toLowerCase().includes('submit') || h.toStatus === 'Pending');
        break;
      case 'L1 Approved':
        matched = history.find(h => h.toStatus === 'CheckerApproved');
        break;
      case 'L2 Authorized':
        matched = history.find(h => h.toStatus === 'Authorized');
        break;
      case 'Complete':
        matched = history.find(h => h.toStatus === 'Authorized');
        break;
    }
    return matched;
  };

  return (
    <div>
      <h4 className="text-xs font-bold text-slate-600 mb-3">Approval Workflow</h4>
      <div className="flex items-start">
        {EXPECTED_FLOW.map((step, idx) => {
          const state = getStepState(step);
          const meta = getStepMeta(step);
          return (
            <div key={step} className="flex items-start flex-1">
              <div className="flex flex-col items-center text-center flex-1">
                <div
                  className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold
                    ${state === 'completed' ? 'bg-green-100 text-green-700' :
                      state === 'current' ? 'bg-amber-100 text-amber-700' :
                      'bg-slate-100 text-slate-400'}`}
                >
                  {state === 'completed' ? '✅' : state === 'current' ? '⏳' : '○'}
                </div>
                <p className="text-xs font-semibold text-slate-700 mt-1">{step}</p>
                {meta && (
                  <div className="text-[10px] text-slate-400 mt-0.5 leading-tight">
                    <span>{meta.performedBy}</span>
                    <br />
                    <span>{fmtDate(meta.performedAt)}</span>
                  </div>
                )}
              </div>
              {idx < EXPECTED_FLOW.length - 1 && (
                <div className="flex-shrink-0 mt-4 w-8">
                  <div className={`h-0.5 w-full ${state === 'completed' ? 'bg-green-300' : 'bg-slate-200'}`} />
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─── Tab Switcher ────────────────────────────────────────────────────────────

type TabKey = 'single' | 'batch' | 'cases' | 'dashboard';

const TABS: { key: TabKey; label: string; icon: typeof ShieldCheck }[] = [
  { key: 'single', label: 'Single Policy', icon: Search },
  { key: 'batch', label: 'Batch', icon: FileSpreadsheet },
  { key: 'cases', label: 'Cases', icon: Filter },
  { key: 'dashboard', label: 'Dashboard', icon: ShieldCheck },
];

function TabSwitcher({ active, onChange }: { active: TabKey; onChange: (t: TabKey) => void }) {
  return (
    <div className="flex gap-1 bg-slate-100 rounded-xl p-1">
      {TABS.map(t => {
        const Icon = t.icon;
        const isActive = active === t.key;
        return (
          <button
            key={t.key}
            onClick={() => onChange(t.key)}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition-all
              ${isActive ? 'bg-white text-[#004282] shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}
          >
            <Icon size={15} />
            {t.label}
          </button>
        );
      })}
    </div>
  );
}

// ─── Single Policy Search & 2-Level Approval ─────────────────────────────────

function SinglePolicyTab() {
  const [policyNumber, setPolicyNumber] = useState('');
  const [payoutType, setPayoutType] = useState('Maturity');
  const [result, setResult] = useState<PayoutCase | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [remarks, setRemarks] = useState('');
  const [deciding, setDeciding] = useState(false);
  const [decisionMsg, setDecisionMsg] = useState<string | null>(null);
  const [calcDetailsOpen, setCalcDetailsOpen] = useState(false);

  const handleSearch = async () => {
    if (!policyNumber.trim()) return;
    setLoading(true);
    setError(null);
    setResult(null);
    setDecisionMsg(null);
    try {
      const res = await api.post<PayoutCase>(PAYOUT_ROUTE.search, {
        policyNumber: policyNumber.trim(),
        payoutType,
      });
      setResult(res.data);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } };
      setError(err?.response?.data?.error || 'Search failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleDecision = async (action: 'checkerApprove' | 'checkerReject' | 'authorizerApprove' | 'authorizerReject') => {
    if (!result) return;
    setDeciding(true);
    setDecisionMsg(null);
    try {
      const routeMap = {
        checkerApprove: PAYOUT_ROUTE.checkerApprove,
        checkerReject: PAYOUT_ROUTE.checkerReject,
        authorizerApprove: PAYOUT_ROUTE.authorizerApprove,
        authorizerReject: PAYOUT_ROUTE.authorizerReject,
      };
      const res = await api.post<PayoutCase>(routeMap[action], {
        caseId: result.id,
        remarks: remarks || undefined,
      });
      setResult(res.data);
      const labels = {
        checkerApprove: 'Checker approved',
        checkerReject: 'Checker rejected',
        authorizerApprove: 'Authorized',
        authorizerReject: 'Rejected by authorizer',
      };
      setDecisionMsg(`✅ ${labels[action]} successfully.`);
      setRemarks('');
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      setDecisionMsg(`❌ ${err?.response?.data?.message || 'Action failed.'}`);
    } finally {
      setDeciding(false);
    }
  };

  return (
    <div className="space-y-4">
      {/* Search form */}
      <div className={`${CARD_CLS} p-5`}>
        <h3 className="text-sm font-bold text-[#004282] mb-3">Search Policy</h3>
        <div className="flex flex-wrap gap-3 items-end">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-xs font-semibold text-slate-600 mb-1">Policy Number</label>
            <input
              value={policyNumber}
              onChange={e => setPolicyNumber(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleSearch()}
              placeholder="Enter policy number"
              className={INPUT_CLS}
            />
          </div>
          <div className="min-w-[160px]">
            <label className="block text-xs font-semibold text-slate-600 mb-1">Payout Type</label>
            <select value={payoutType} onChange={e => setPayoutType(e.target.value)} className={INPUT_CLS}>
              <option value="Maturity">Maturity</option>
              <option value="Surrender">Surrender</option>
              <option value="DeathClaim">Death Claim</option>
            </select>
          </div>
          <button onClick={handleSearch} disabled={loading || !policyNumber.trim()} className={`${BTN_PRIMARY} px-5 py-2`}>
            {loading ? <Spinner /> : <Search size={15} />}
            Search
          </button>
        </div>
      </div>

      {error && <ErrBanner msg={error} />}

      {/* Result card */}
      {result && (
        <div className={`${CARD_CLS} p-5 space-y-4`}>
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-[#004282]">Verification Result</h3>
            <StatusBadge status={result.status} />
          </div>

          {/* Info grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs">
            <div><span className="text-slate-500">Policy:</span> <strong>{result.policyNumber}</strong></div>
            <div><span className="text-slate-500">Product:</span> <strong>{result.productName}</strong></div>
            <div><span className="text-slate-500">UIN:</span> <strong>{result.uin}</strong></div>
            <div><span className="text-slate-500">Type:</span> <strong>{result.payoutType}</strong></div>
          </div>

          {/* Amount comparison */}
          <div className={`grid grid-cols-3 gap-4 p-4 rounded-xl ${varianceRowColor(result.variancePct)}`}>
            <div className="text-center">
              <p className="text-xs text-slate-500 mb-1">Core System</p>
              <p className="text-lg font-bold text-slate-800">₹{INR(result.coreSystemAmount)}</p>
            </div>
            <div className="text-center">
              <p className="text-xs text-slate-500 mb-1">PrecisionPro</p>
              <p className="text-lg font-bold text-slate-800">₹{INR(result.precisionProAmount)}</p>
            </div>
            <div className="text-center">
              <p className="text-xs text-slate-500 mb-1">Variance</p>
              <p className="text-lg font-bold text-slate-800">
                ₹{INR(result.variance)} <span className="text-xs">({result.variancePct}%)</span>
              </p>
            </div>
          </div>

          {/* Calculation Details (collapsible) */}
          <div className="border border-slate-200 rounded-xl">
            <button
              onClick={() => setCalcDetailsOpen(!calcDetailsOpen)}
              className="w-full flex items-center justify-between px-4 py-3 text-xs font-bold text-[#004282] hover:bg-slate-50 transition rounded-xl"
            >
              <span>Calculation Details</span>
              <ChevronDown size={14} className={`transition ${calcDetailsOpen ? 'rotate-180' : ''}`} />
            </button>
            {calcDetailsOpen && (
              <div className="px-4 pb-4 space-y-3 text-xs">
                {/* Formula versions */}
                <div>
                  <h5 className="font-semibold text-slate-600 mb-1">Formula Info</h5>
                  <div className="grid grid-cols-3 gap-2">
                    <div className="bg-slate-50 rounded-lg p-2">
                      <span className="text-slate-400 block">Product Version</span>
                      <strong>{result.productVersion ?? '—'}</strong>
                    </div>
                    <div className="bg-slate-50 rounded-lg p-2">
                      <span className="text-slate-400 block">Factor Version</span>
                      <strong>{result.factorVersion ?? '—'}</strong>
                    </div>
                    <div className="bg-slate-50 rounded-lg p-2">
                      <span className="text-slate-400 block">Formula Version</span>
                      <strong>{result.formulaVersion ?? '—'}</strong>
                    </div>
                  </div>
                </div>

                {/* Parameters table */}
                <div>
                  <h5 className="font-semibold text-slate-600 mb-1">Parameters</h5>
                  <table className="w-full text-xs border border-slate-100 rounded-lg overflow-hidden">
                    <thead className="bg-slate-50">
                      <tr>
                        <th className="px-3 py-1.5 text-left text-slate-600">Parameter</th>
                        <th className="px-3 py-1.5 text-right text-slate-600">Value</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr className="border-t border-slate-100">
                        <td className="px-3 py-1.5">Annual Premium</td>
                        <td className="px-3 py-1.5 text-right font-mono">{result.annualPremium != null ? `₹${INR(result.annualPremium)}` : '—'}</td>
                      </tr>
                      <tr className="border-t border-slate-100">
                        <td className="px-3 py-1.5">Policy Term</td>
                        <td className="px-3 py-1.5 text-right font-mono">{result.policyTerm != null ? `${result.policyTerm} years` : '—'}</td>
                      </tr>
                      <tr className="border-t border-slate-100">
                        <td className="px-3 py-1.5">Premium Paying Term</td>
                        <td className="px-3 py-1.5 text-right font-mono">{result.premiumPayingTerm != null ? `${result.premiumPayingTerm} years` : '—'}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>

                {/* Calculated result */}
                <div className="flex items-center justify-between bg-blue-50 rounded-lg p-3">
                  <span className="font-semibold text-blue-700">Calculated PrecisionPro Amount</span>
                  <span className="text-lg font-bold text-blue-800">₹{INR(result.precisionProAmount)}</span>
                </div>

                {/* Source & timestamp */}
                <div className="flex gap-4 text-slate-500">
                  <span>Source: <strong className="text-slate-700">{result.calculationSource ?? '—'}</strong></span>
                  {result.calculatedAt && <span>Calculated At: <strong className="text-slate-700">{fmtDate(result.calculatedAt)}</strong></span>}
                </div>
              </div>
            )}
          </div>

          {/* Workflow stepper */}
          <WorkflowStepper history={result.workflowHistory} status={result.status} />

          {/* Decision actions */}
          {(result.status === 'Pending' || result.status === 'CheckerApproved') && (
            <div className="border-t pt-4 space-y-3">
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Remarks (optional)</label>
                <input value={remarks} onChange={e => setRemarks(e.target.value)} className={INPUT_CLS} placeholder="Add remarks..." />
              </div>
              <div className="flex gap-2">
                {result.status === 'Pending' && (
                  <>
                    <button onClick={() => handleDecision('checkerApprove')} disabled={deciding} className={`${BTN_PRIMARY} px-4 py-2`}>
                      {deciding ? <Spinner /> : <CheckCircle2 size={14} />} Checker Approve
                    </button>
                    <button onClick={() => handleDecision('checkerReject')} disabled={deciding}
                      className="bg-red-600 text-white rounded-xl font-semibold text-sm hover:bg-red-700 disabled:opacity-60 transition px-4 py-2 flex items-center gap-2">
                      Checker Reject
                    </button>
                  </>
                )}
                {result.status === 'CheckerApproved' && (
                  <>
                    <button onClick={() => handleDecision('authorizerApprove')} disabled={deciding} className={`${BTN_PRIMARY} px-4 py-2`}>
                      {deciding ? <Spinner /> : <CheckCircle2 size={14} />} Authorize
                    </button>
                    <button onClick={() => handleDecision('authorizerReject')} disabled={deciding}
                      className="bg-red-600 text-white rounded-xl font-semibold text-sm hover:bg-red-700 disabled:opacity-60 transition px-4 py-2 flex items-center gap-2">
                      Reject
                    </button>
                  </>
                )}
              </div>
            </div>
          )}

          {decisionMsg && (
            <p className={`text-sm font-medium ${decisionMsg.startsWith('✅') ? 'text-green-600' : 'text-red-600'}`}>
              {decisionMsg}
            </p>
          )}
        </div>
      )}
    </div>
  );
}

// ─── Batch Tab: System Generate + File Upload ────────────────────────────────

function BatchTab() {
  const [batchMode, setBatchMode] = useState<'generate' | 'upload'>('generate');
  const [payoutType, setPayoutType] = useState('Maturity');
  const [maxRecords, setMaxRecords] = useState(10);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [batchResult, setBatchResult] = useState<PayoutBatch | null>(null);
  const [batchCases, setBatchCases] = useState<PayoutCase[]>([]);
  const [batches, setBatches] = useState<PayoutBatch[]>([]);
  const [showMismatchOnly, setShowMismatchOnly] = useState(false);

  const loadBatches = useCallback(async () => {
    try {
      const res = await api.get<PayoutBatch[]>(PAYOUT_ROUTE.batches);
      setBatches(res.data);
    } catch { /* ignore */ }
  }, []);

  useEffect(() => { loadBatches(); }, [loadBatches]);

  const handleGenerate = async () => {
    setLoading(true);
    setError(null);
    setBatchResult(null);
    setBatchCases([]);
    try {
      const res = await api.post<PayoutBatch>(PAYOUT_ROUTE.batchGenerate, { payoutType, maxRecords });
      setBatchResult(res.data);
      const casesRes = await api.get<PayoutCase[]>(`${PAYOUT_ROUTE.batches}/${res.data.id}/cases`);
      setBatchCases(casesRes.data);
      loadBatches();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } };
      setError(err?.response?.data?.error || 'Batch generation failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpload = async (file: File) => {
    setLoading(true);
    setError(null);
    setBatchResult(null);
    setBatchCases([]);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const res = await api.post<PayoutBatch>(`${PAYOUT_ROUTE.upload}?payoutType=${payoutType}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      setBatchResult(res.data);
      const casesRes = await api.get<PayoutCase[]>(`${PAYOUT_ROUTE.batches}/${res.data.id}/cases`);
      setBatchCases(casesRes.data);
      loadBatches();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } };
      setError(err?.response?.data?.error || 'Upload failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleExport = async (format: string) => {
    try {
      const res = await api.get(
        `${PAYOUT_ROUTE.export}?format=${format}${batchResult ? `&batchId=${batchResult.id}` : ''}`,
        { responseType: 'blob' }
      );
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `payout_export.${format.toLowerCase()}`;
      a.click();
      window.URL.revokeObjectURL(url);
    } catch { /* ignore */ }
  };

  return (
    <div className="space-y-4">
      {/* Mode toggle */}
      <div className={`${CARD_CLS} p-5`}>
        <div className="flex gap-3 mb-4">
          <button
            onClick={() => setBatchMode('generate')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition-all
              ${batchMode === 'generate' ? 'bg-[#004282] text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}
          >
            <FileCog size={15} /> System Generate
          </button>
          <button
            onClick={() => setBatchMode('upload')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition-all
              ${batchMode === 'upload' ? 'bg-[#004282] text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}
          >
            <Upload size={15} /> File Upload
          </button>
        </div>

        {batchMode === 'generate' ? (
          <div className="flex flex-wrap gap-3 items-end">
            <div className="min-w-[160px]">
              <label className="block text-xs font-semibold text-slate-600 mb-1">Payout Type</label>
              <select value={payoutType} onChange={e => setPayoutType(e.target.value)} className={INPUT_CLS}>
                <option value="Maturity">Maturity</option>
                <option value="Surrender">Surrender</option>
                <option value="DeathClaim">Death Claim</option>
              </select>
            </div>
            <div className="w-28">
              <label className="block text-xs font-semibold text-slate-600 mb-1">Max Records</label>
              <input type="number" value={maxRecords} onChange={e => setMaxRecords(Number(e.target.value))} min={1} max={100} className={INPUT_CLS} />
            </div>
            <button onClick={handleGenerate} disabled={loading} className={`${BTN_PRIMARY} px-5 py-2`}>
              {loading ? <Spinner /> : <FileCog size={15} />} Generate Batch
            </button>
          </div>
        ) : (
          <div className="space-y-3">
            <div className="flex gap-3 items-end">
              <div className="min-w-[160px]">
                <label className="block text-xs font-semibold text-slate-600 mb-1">Payout Type</label>
                <select value={payoutType} onChange={e => setPayoutType(e.target.value)} className={INPUT_CLS}>
                  <option value="Maturity">Maturity</option>
                  <option value="Surrender">Surrender</option>
                  <option value="DeathClaim">Death Claim</option>
                </select>
              </div>
              <a href={PAYOUT_ROUTE.template} download className="text-xs text-blue-600 underline flex items-center gap-1">
                <Download size={12} /> Download Template
              </a>
            </div>
            <div className="flex items-center gap-3">
              <label className={`${BTN_PRIMARY} px-5 py-2 cursor-pointer`}>
                <Upload size={15} /> Upload CSV
                <input
                  type="file"
                  accept=".csv,.xlsx"
                  className="hidden"
                  onChange={e => { if (e.target.files?.[0]) handleUpload(e.target.files[0]); }}
                />
              </label>
              {loading && <Spinner />}
            </div>
          </div>
        )}
      </div>

      {error && <ErrBanner msg={error} />}

      {/* Batch result */}
      {batchResult && (
        <div className={`${CARD_CLS} p-5 space-y-4`}>
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-[#004282]">
              Batch #{batchResult.id} — {batchResult.batchType}
            </h3>
            <div className="flex gap-2">
              <button onClick={() => handleExport('CSV')} className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-slate-100 text-xs font-semibold hover:bg-slate-200 transition">
                <Download size={12} /> CSV
              </button>
              <button onClick={() => handleExport('JSON')} className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-slate-100 text-xs font-semibold hover:bg-slate-200 transition">
                <FileJson size={12} /> JSON
              </button>
            </div>
          </div>

          {/* Summary stat cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs text-center">
            <div className="bg-slate-100 border border-slate-200 rounded-xl p-3">
              <span className="text-slate-500 block mb-1">Total</span>
              <strong className="text-lg text-slate-800">{batchResult.totalCount}</strong>
            </div>
            <div className="bg-green-50 border border-green-200 rounded-xl p-3">
              <span className="text-green-600 block mb-1">Match</span>
              <strong className="text-lg text-green-700">{batchResult.matchCount}</strong>
            </div>
            <div className="bg-red-50 border border-red-200 rounded-xl p-3">
              <span className="text-red-600 block mb-1">Mismatch</span>
              <strong className="text-lg text-red-700">{batchResult.mismatchCount}</strong>
            </div>
            <div className="bg-slate-50 border border-slate-200 rounded-xl p-3">
              <span className="text-slate-500 block mb-1">Processed</span>
              <strong className="text-lg text-slate-800">{batchResult.processedCount}</strong>
            </div>
          </div>

          {/* Filter & Mismatch download */}
          <div className="flex items-center gap-3">
            <button
              onClick={() => setShowMismatchOnly(!showMismatchOnly)}
              className={`flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-semibold transition-all ${
                showMismatchOnly ? 'bg-red-600 text-white hover:bg-red-700' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
              }`}
            >
              <Filter size={12} />
              {showMismatchOnly ? 'Show All Cases' : 'Show Mismatches Only'}
            </button>
            {batchResult.mismatchCount > 0 && (
              <button
                onClick={() => {
                  const mismatched = batchCases.filter(c => Math.abs(c.variancePct) > 1);
                  const header = 'Policy,Product,Core Amount,PP Amount,Variance,Variance %,Status\n';
                  const rows = mismatched.map(c =>
                    `${c.policyNumber},${c.productName},${c.coreSystemAmount},${c.precisionProAmount},${c.variance},${c.variancePct}%,${c.status}`
                  ).join('\n');
                  const blob = new Blob([header + rows], { type: 'text/csv' });
                  const url = window.URL.createObjectURL(blob);
                  const a = document.createElement('a');
                  a.href = url;
                  a.download = `mismatch_report_batch_${batchResult.id}.csv`;
                  a.click();
                  window.URL.revokeObjectURL(url);
                }}
                className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-semibold bg-red-50 text-red-700 hover:bg-red-100 transition"
              >
                <Download size={12} /> Download Mismatch Report
              </button>
            )}
          </div>

          {/* Batch cases table */}
          {batchCases.length > 0 && (
            <div className="overflow-x-auto rounded-xl border border-slate-100">
              <table className="w-full text-xs">
                <thead className="bg-slate-50 text-slate-600">
                  <tr>
                    <th className="px-3 py-2 text-left">Policy</th>
                    <th className="px-3 py-2 text-left">Product</th>
                    <th className="px-3 py-2 text-right">Core Amt</th>
                    <th className="px-3 py-2 text-right">PP Amt</th>
                    <th className="px-3 py-2 text-right">Variance %</th>
                    <th className="px-3 py-2 text-center">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {(showMismatchOnly ? batchCases.filter(c => Math.abs(c.variancePct) > 1) : batchCases).map(c => (
                    <tr key={c.id} className={`border-t border-slate-50 ${varianceRowColor(c.variancePct)}`}>
                      <td className="px-3 py-2 font-mono">{c.policyNumber}</td>
                      <td className="px-3 py-2">{c.productName}</td>
                      <td className="px-3 py-2 text-right">₹{INR(c.coreSystemAmount)}</td>
                      <td className="px-3 py-2 text-right">₹{INR(c.precisionProAmount)}</td>
                      <td className="px-3 py-2 text-right">{c.variancePct}%</td>
                      <td className="px-3 py-2 text-center"><StatusBadge status={c.status} /></td>
                    </tr>
                  ))}
                  {showMismatchOnly && batchCases.filter(c => Math.abs(c.variancePct) > 1).length === 0 && (
                    <tr><td colSpan={6} className="text-center py-6 text-slate-400">No mismatched cases found.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Recent batches */}
      {batches.length > 0 && (
        <div className={`${CARD_CLS} p-5`}>
          <h3 className="text-sm font-bold text-[#004282] mb-3">Recent Batches</h3>
          <div className="overflow-x-auto rounded-xl border border-slate-100">
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-slate-600">
                <tr>
                  <th className="px-3 py-2 text-left">ID</th>
                  <th className="px-3 py-2 text-left">Type</th>
                  <th className="px-3 py-2 text-left">File</th>
                  <th className="px-3 py-2 text-right">Total</th>
                  <th className="px-3 py-2 text-right">Match</th>
                  <th className="px-3 py-2 text-right">Mismatch</th>
                  <th className="px-3 py-2 text-center">Status</th>
                  <th className="px-3 py-2 text-left">Date</th>
                </tr>
              </thead>
              <tbody>
                {batches.map(b => (
                  <tr key={b.id} className="border-t border-slate-50">
                    <td className="px-3 py-2 font-mono">#{b.id}</td>
                    <td className="px-3 py-2">{b.batchType}</td>
                    <td className="px-3 py-2">{b.fileName ?? '—'}</td>
                    <td className="px-3 py-2 text-right">{b.totalCount}</td>
                    <td className="px-3 py-2 text-right text-green-600">{b.matchCount}</td>
                    <td className="px-3 py-2 text-right text-red-600">{b.mismatchCount}</td>
                    <td className="px-3 py-2 text-center">{b.status}</td>
                    <td className="px-3 py-2">{fmtDate(b.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Cases Tab ───────────────────────────────────────────────────────────────

function CasesTab() {
  const [cases, setCases] = useState<PayoutCase[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [loading, setLoading] = useState(false);
  const [expanded, setExpanded] = useState<number | null>(null);

  const loadCases = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.append('status', statusFilter);
      const res = await api.get<PayoutCase[]>(`${PAYOUT_ROUTE.cases}?${params}`);
      setCases(res.data);
    } catch { /* ignore */ }
    finally { setLoading(false); }
  }, [statusFilter]);

  useEffect(() => { loadCases(); }, [loadCases]);

  return (
    <div className="space-y-4">
      <div className={`${CARD_CLS} p-5`}>
        <div className="flex items-center gap-3 mb-3">
          <h3 className="text-sm font-bold text-[#004282]">Payout Cases</h3>
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className={`${INPUT_CLS} w-40`}>
            <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="CheckerApproved">Checker Approved</option>
            <option value="CheckerRejected">Checker Rejected</option>
            <option value="Authorized">Authorized</option>
            <option value="Rejected">Rejected</option>
          </select>
          {loading && <Spinner />}
        </div>

        <div className="overflow-x-auto rounded-xl border border-slate-100">
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-3 py-2 text-left">ID</th>
                <th className="px-3 py-2 text-left">Policy</th>
                <th className="px-3 py-2 text-left">Product</th>
                <th className="px-3 py-2 text-left">Type</th>
                <th className="px-3 py-2 text-right">Core Amt</th>
                <th className="px-3 py-2 text-right">PP Amt</th>
                <th className="px-3 py-2 text-right">Var %</th>
                <th className="px-3 py-2 text-center">Status</th>
                <th className="px-3 py-2 text-left">Created</th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              {cases.map(c => (
                <>
                  <tr key={c.id} className={`border-t border-slate-50 cursor-pointer hover:bg-slate-50/80 ${varianceRowColor(c.variancePct)}`}
                      onClick={() => setExpanded(expanded === c.id ? null : c.id)}>
                    <td className="px-3 py-2 font-mono">#{c.id}</td>
                    <td className="px-3 py-2 font-mono">{c.policyNumber}</td>
                    <td className="px-3 py-2">{c.productName}</td>
                    <td className="px-3 py-2">{c.payoutType}</td>
                    <td className="px-3 py-2 text-right">₹{INR(c.coreSystemAmount)}</td>
                    <td className="px-3 py-2 text-right">₹{INR(c.precisionProAmount)}</td>
                    <td className="px-3 py-2 text-right">{c.variancePct}%</td>
                    <td className="px-3 py-2 text-center"><StatusBadge status={c.status} /></td>
                    <td className="px-3 py-2">{fmtDate(c.createdAt)}</td>
                    <td className="px-3 py-2"><ChevronDown size={12} className={`transition ${expanded === c.id ? 'rotate-180' : ''}`} /></td>
                  </tr>
                  {expanded === c.id && (
                    <tr key={`${c.id}-detail`} className="bg-slate-50/60">
                      <td colSpan={10} className="px-4 py-3">
                        <div className="text-xs space-y-2">
                          <div className="flex gap-4">
                            <span><strong>Input:</strong> {c.inputMode}</span>
                            <span><strong>UIN:</strong> {c.uin}</span>
                            <span><strong>Source:</strong> {c.calculationSource}</span>
                            {c.remarks && <span><strong>Remarks:</strong> {c.remarks}</span>}
                          </div>
                          {c.workflowHistory.length > 0 && (
                            <div>
                              <strong>Workflow:</strong>
                              <div className="flex items-center gap-2 mt-1">
                                {c.workflowHistory.map((step, idx) => (
                                  <div key={step.id} className="flex items-center gap-2">
                                    {idx > 0 && <ArrowRight size={10} className="text-slate-400" />}
                                    <span className="px-2 py-0.5 rounded bg-white border border-slate-200">
                                      {step.action} <span className="text-slate-400">by {step.performedBy}</span>
                                    </span>
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}
                        </div>
                      </td>
                    </tr>
                  )}
                </>
              ))}
              {cases.length === 0 && !loading && (
                <tr><td colSpan={10} className="text-center py-8 text-slate-400">No cases found.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// ─── Dashboard Tab ───────────────────────────────────────────────────────────

function DashboardTab() {
  const [dashboard, setDashboard] = useState<PayoutDashboard | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const res = await api.get<PayoutDashboard>(PAYOUT_ROUTE.dashboard);
        setDashboard(res.data);
      } catch { /* ignore */ }
      finally { setLoading(false); }
    })();
  }, []);

  if (loading) return <div className="flex justify-center py-16"><Spinner size={24} /></div>;
  if (!dashboard) return <ErrBanner msg="Failed to load dashboard." />;

  const cards = [
    { label: 'Total This Month', value: dashboard.totalThisMonth, bg: 'bg-slate-50', fg: 'text-slate-800' },
    { label: 'Pending', value: dashboard.pendingCount, bg: 'bg-amber-50', fg: 'text-amber-700' },
    { label: 'Checker Approved', value: dashboard.checkerApprovedCount, bg: 'bg-blue-50', fg: 'text-blue-700' },
    { label: 'Authorized', value: dashboard.authorizedCount, bg: 'bg-green-50', fg: 'text-green-700' },
    { label: 'Rejected', value: dashboard.rejectedCount, bg: 'bg-red-50', fg: 'text-red-700' },
    { label: 'Match', value: dashboard.matchCount, bg: 'bg-green-50', fg: 'text-green-700' },
    { label: 'Mismatch', value: dashboard.mismatchCount, bg: 'bg-red-50', fg: 'text-red-700' },
    { label: 'Total Variance', value: `₹${INR(dashboard.totalVariance)}`, bg: 'bg-slate-50', fg: 'text-slate-800' },
  ];

  return (
    <div className="space-y-4">
      {/* Pending Approvals Alert */}
      {(dashboard.pendingCount > 0 || dashboard.checkerApprovedCount > 0) && (
        <div className="flex items-center justify-between bg-amber-50 border border-amber-200 rounded-xl px-5 py-4">
          <div className="flex items-center gap-3">
            <AlertCircle size={18} className="text-amber-600" />
            <p className="text-sm font-semibold text-amber-800">
              {dashboard.pendingCount + dashboard.checkerApprovedCount} cases pending your approval
            </p>
          </div>
          <button className={`${BTN_PRIMARY} px-4 py-2`}>Review Now</button>
        </div>
      )}

      <div className={`${CARD_CLS} p-5`}>
        <h3 className="text-sm font-bold text-[#004282] mb-4">Payout Dashboard — This Month</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {cards.map(c => (
            <div key={c.label} className={`${c.bg} rounded-xl p-4 text-center`}>
              <p className="text-xs text-slate-500 mb-1">{c.label}</p>
              <p className={`text-xl font-bold ${c.fg}`}>{c.value}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ─── Main export ─────────────────────────────────────────────────────────────

export default function PayoutVerification(): JSX.Element {
  const [activeTab, setActiveTab] = useState<TabKey>('single');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Payout Verification
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          Verify policy payouts with 2-level approval workflow (Checker → Authorizer).
        </p>
      </div>

      {/* Tab switcher */}
      <TabSwitcher active={activeTab} onChange={setActiveTab} />

      {/* Active tab content */}
      {activeTab === 'single' && <SinglePolicyTab />}
      {activeTab === 'batch' && <BatchTab />}
      {activeTab === 'cases' && <CasesTab />}
      {activeTab === 'dashboard' && <DashboardTab />}
    </div>
  );
}

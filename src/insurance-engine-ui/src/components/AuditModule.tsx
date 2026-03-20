import { useState, useEffect, useCallback, useRef, type JSX } from 'react';
import {
  ShieldCheck, Upload, Download, Check, X, Search,
  FileSpreadsheet, AlertCircle, CheckCircle2,
  Filter, ChevronDown,
} from 'lucide-react';
import axios from 'axios';

// ─── Constants & helpers ─────────────────────────────────────────────────────

const API_URL = import.meta.env.VITE_API_URL || 'http://ezytek1706-003-site3.rtempurl.com';
const AUDIT_TYPES = {
  payoutVerification: 'PayoutVerification',
  additionBonus: 'AdditionBonus',
} as const;
const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 2 });
const INPUT_CLS = 'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#007bff]';
const CARD_CLS = 'bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)]';
const BTN_PRIMARY = 'bg-[#004282] text-white rounded-xl font-semibold text-sm hover:bg-[#003370] disabled:opacity-60 transition flex items-center justify-center gap-2';

const fmtDate = (d: string) =>
  new Date(d).toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

function auditTypeLabel(sub: AuditSubModule) {
  return sub === 'payout-verification' ? AUDIT_TYPES.payoutVerification : AUDIT_TYPES.additionBonus;
}

function subTitle(sub: AuditSubModule) {
  return sub === 'payout-verification' ? 'Payout Verification' : 'Addition / Bonus';
}

// ─── Interfaces ──────────────────────────────────────────────────────────────

interface AuditCase {
  caseId: string;
  policyNumber: string;
  productName: string;
  uin: string;
  policyAnniversary: string;
  coreSystemAmount: number;
  precisionProAmount: number;
  variance: number;
  status: 'Pending' | 'Approved' | 'Rejected';
  dateSearched?: string;
}

interface BatchRecord {
  batchId: string;
  uploadDate: string;
  fileName: string;
  auditType: string;
  totalCases: number;
  processed: number;
  pending: number;
  status: string;
}

interface AuditLogEntry {
  logId: string;
  eventType: string;
  caseId: string;
  doneBy: string;
  doneAt: string;
  details: string;
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
    s === 'approved' || s === 'match' ? 'bg-green-100 text-green-700' :
    s === 'rejected' || s === 'mismatch' ? 'bg-red-100 text-[#d32f2f]' :
    'bg-amber-100 text-amber-700';
  return <span className={`inline-block px-3 py-0.5 rounded-full text-xs font-bold ${cls}`}>{status}</span>;
}

function varianceRowColor(variance: number) {
  const abs = Math.abs(variance);
  if (abs <= 1000) return 'bg-green-50/60';
  if (abs <= 5000) return 'bg-amber-50/60';
  return 'bg-red-50/60';
}

// ─── Tab Switcher ────────────────────────────────────────────────────────────

const TAB_OPTIONS: { key: AuditSubOption; label: string; icon: typeof ShieldCheck }[] = [
  { key: 'single', label: 'Single Policy', icon: Search },
  { key: 'excel', label: 'Excel Upload', icon: FileSpreadsheet },
];

function TabSwitcher({ active, onChange }: { active: AuditSubOption; onChange: (t: AuditSubOption) => void }) {
  return (
    <div className="flex gap-1 bg-slate-100 rounded-xl p-1">
      {TAB_OPTIONS.map(t => {
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

// ─── Single Policy ───────────────────────────────────────────────────────────

function SinglePolicy({ sub }: { sub: AuditSubModule }) {
  const [policyNumber, setPolicyNumber] = useState('');
  const [result, setResult] = useState<AuditCase | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [remarks, setRemarks] = useState('');
  const [deciding, setDeciding] = useState(false);
  const [decisionMsg, setDecisionMsg] = useState<string | null>(null);

  const handleSearch = async () => {
    if (!policyNumber.trim()) return;
    setLoading(true);
    setError(null);
    setResult(null);
    setDecisionMsg(null);
    try {
      const res = await axios.post(`${API_URL}/api/audit/search`, {
        policyNumber: policyNumber.trim(),
        auditType: auditTypeLabel(sub),
      });
      setResult(res.data);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } };
      setError(err?.response?.data?.error || 'Search failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleDecision = async (decision: 'approve' | 'reject') => {
    if (!result) return;
    setDeciding(true);
    setDecisionMsg(null);
    try {
      await axios.post(`${API_URL}/api/audit/${decision}`, {
        caseId: result.caseId,
        remarks: remarks.trim(),
      });
      setDecisionMsg(`Case ${decision === 'approve' ? 'approved' : 'rejected'} successfully.`);
      setResult(r => r ? { ...r, status: decision === 'approve' ? 'Approved' : 'Rejected' } : r);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } };
      setError(err?.response?.data?.error || `Failed to ${decision} case.`);
    } finally {
      setDeciding(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Search bar */}
      <div className={`${CARD_CLS} p-6`}>
        <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-4">Search Policy</h3>
        <div className="flex gap-3">
          <input
            type="text"
            value={policyNumber}
            onChange={e => setPolicyNumber(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleSearch()}
            className={`${INPUT_CLS} max-w-md`}
            placeholder="Enter Policy Number"
          />
          <button onClick={handleSearch} disabled={loading || !policyNumber.trim()} className={`px-6 py-2 ${BTN_PRIMARY}`}>
            {loading
              ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              : <Search size={15} />}
            {loading ? 'Searching…' : 'Search'}
          </button>
        </div>
        {error && <div className="mt-3"><ErrBanner msg={error} /></div>}
      </div>

      {/* Result card */}
      {result && (
        <div className={`${CARD_CLS} p-6 space-y-5`}>
          <div className="flex items-center gap-3">
            <ShieldCheck size={24} className="text-[#004282]" />
            <div>
              <h3 className="text-lg font-bold text-[#004282]">Audit Result</h3>
              <StatusBadge status={result.status} />
            </div>
          </div>

          <div className={`grid sm:grid-cols-2 lg:grid-cols-4 gap-4 text-sm rounded-xl p-4 ${varianceRowColor(result.variance)}`}>
            {[
              { label: 'Policy Number', value: result.policyNumber },
              { label: 'Product Name', value: result.productName },
              { label: 'UIN', value: result.uin },
              { label: 'Policy Anniversary', value: result.policyAnniversary },
              { label: 'Core System', value: `₹ ${INR(result.coreSystemAmount)}` },
              { label: 'Precision Pro', value: `₹ ${INR(result.precisionProAmount)}` },
              { label: 'Variance', value: `₹ ${INR(result.variance)}` },
              { label: 'Status', value: result.status },
            ].map(row => (
              <div key={row.label} className="bg-white/80 rounded-lg p-3">
                <p className="text-xs text-slate-400 uppercase tracking-wider">{row.label}</p>
                <p className="font-bold text-[#004282] mt-0.5">{row.value}</p>
              </div>
            ))}
          </div>

          {/* Approve / Reject controls */}
          {result.status === 'Pending' && (
            <div className="space-y-3">
              <textarea
                value={remarks}
                onChange={e => setRemarks(e.target.value)}
                className={`${INPUT_CLS} h-20`}
                placeholder="Optional remarks…"
              />
              <div className="flex gap-3">
                <button
                  onClick={() => handleDecision('approve')}
                  disabled={deciding}
                  className="px-6 py-2 bg-green-600 text-white rounded-xl font-semibold text-sm
                             hover:bg-green-700 disabled:opacity-60 transition flex items-center gap-2"
                >
                  <Check size={15} /> Approve
                </button>
                <button
                  onClick={() => handleDecision('reject')}
                  disabled={deciding}
                  className="px-6 py-2 bg-[#d32f2f] text-white rounded-xl font-semibold text-sm
                             hover:bg-red-800 disabled:opacity-60 transition flex items-center gap-2"
                >
                  <X size={15} /> Reject
                </button>
              </div>
            </div>
          )}

          {decisionMsg && (
            <div className="flex items-start gap-2 p-3 bg-green-50 border border-green-200 rounded-xl text-xs text-green-700">
              <CheckCircle2 size={14} className="mt-0.5 flex-shrink-0" />
              {decisionMsg}
            </div>
          )}
        </div>
      )}

      {/* Empty state */}
      {!result && !loading && !error && (
        <div className={`${CARD_CLS} flex items-center justify-center h-48 text-slate-400 text-sm`}>
          Enter a policy number and click <strong className="mx-1">Search</strong> to begin.
        </div>
      )}
    </div>
  );
}

// ─── Excel Upload ────────────────────────────────────────────────────────────

function ExcelUpload({ sub }: { sub: AuditSubModule }) {
  const auditType = auditTypeLabel(sub);
  const fileRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [results, setResults] = useState<AuditCase[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [error, setError] = useState<string | null>(null);
  const [bulkRemarks, setBulkRemarks] = useState('');
  const [confirmAction, setConfirmAction] = useState<'approve' | 'reject' | null>(null);
  const [deciding, setDeciding] = useState(false);
  const [decisionMsg, setDecisionMsg] = useState<string | null>(null);

  const handleDownloadTemplate = async () => {
    try {
      const res = await axios.get(`${API_URL}/api/audit/template`, {
        params: { auditType },
        responseType: 'blob',
      });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `audit-template-${auditType}.csv`;
      a.click();
      window.URL.revokeObjectURL(url);
    } catch {
      setError('Failed to download template.');
    }
  };

  const handleUpload = async () => {
    if (!file) return;
    setUploading(true);
    setError(null);
    setResults([]);
    setSelected(new Set());
    setDecisionMsg(null);
    setProgress(0);
    try {
      const fd = new FormData();
      fd.append('file', file);
      const res = await axios.post(`${API_URL}/api/audit/upload?auditType=${auditType}`, fd, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: e => {
          if (e.total) setProgress(Math.round((e.loaded / e.total) * 100));
        },
      });
      setResults(res.data);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } };
      setError(err?.response?.data?.error || 'Upload failed.');
    } finally {
      setUploading(false);
    }
  };

  const toggleSelect = (caseId: string) => {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(caseId)) next.delete(caseId); else next.add(caseId);
      return next;
    });
  };

  const toggleAll = () => {
    if (selected.size === results.length) setSelected(new Set());
    else setSelected(new Set(results.map(r => r.caseId)));
  };

  const handleBulkDecision = async () => {
    if (!confirmAction || selected.size === 0) return;
    setDeciding(true);
    setDecisionMsg(null);
    try {
      await axios.post(`${API_URL}/api/audit/bulk-decision`, {
        caseIds: Array.from(selected),
        decision: confirmAction === 'approve' ? 'Approved' : 'Rejected',
        remarks: bulkRemarks.trim(),
      });
      setResults(prev =>
        prev.map(r =>
          selected.has(r.caseId) ? { ...r, status: confirmAction === 'approve' ? 'Approved' as const : 'Rejected' as const } : r
        )
      );
      setDecisionMsg(`${selected.size} case(s) ${confirmAction === 'approve' ? 'approved' : 'rejected'} successfully.`);
      setSelected(new Set());
    } catch (e: unknown) {
      const err = e as { response?: { data?: { error?: string } } };
      setError(err?.response?.data?.error || 'Bulk action failed.');
    } finally {
      setDeciding(false);
      setConfirmAction(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Upload controls */}
      <div className={`${CARD_CLS} p-6 space-y-4`}>
        <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">Upload Audit File</h3>

        <div className="flex flex-wrap gap-3 items-center">
          <button onClick={handleDownloadTemplate} className={`px-5 py-2 ${BTN_PRIMARY}`}>
            <Download size={15} /> Download Template
          </button>

          <div className="flex items-center gap-2">
            <input
              ref={fileRef}
              type="file"
              accept=".csv,.xlsx,.xls"
              onChange={e => setFile(e.target.files?.[0] ?? null)}
              className="text-sm text-slate-600 file:mr-3 file:py-2 file:px-4 file:rounded-xl file:border-0
                         file:text-sm file:font-semibold file:bg-blue-50 file:text-[#004282] hover:file:bg-blue-100"
            />
          </div>

          <button onClick={handleUpload} disabled={!file || uploading} className={`px-5 py-2 ${BTN_PRIMARY}`}>
            {uploading
              ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              : <Upload size={15} />}
            {uploading ? 'Processing…' : 'Process'}
          </button>
        </div>

        {uploading && (
          <div className="space-y-1">
            <div className="w-full bg-slate-100 rounded-full h-2">
              <div className="bg-[#007bff] h-2 rounded-full transition-all" style={{ width: `${progress}%` }} />
            </div>
            <p className="text-xs text-slate-400">{progress}% uploaded</p>
          </div>
        )}

        {error && <ErrBanner msg={error} />}
        {decisionMsg && (
          <div className="flex items-start gap-2 p-3 bg-green-50 border border-green-200 rounded-xl text-xs text-green-700">
            <CheckCircle2 size={14} className="mt-0.5 flex-shrink-0" />
            {decisionMsg}
          </div>
        )}
      </div>

      {/* Results table */}
      {results.length > 0 && (
        <div className={`${CARD_CLS} p-6 space-y-4`}>
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">
              Upload Results — {results.length} case(s)
            </h3>
            <div className="flex gap-2">
              <button
                onClick={() => setConfirmAction('approve')}
                disabled={selected.size === 0}
                className="px-4 py-1.5 bg-green-600 text-white rounded-xl text-xs font-semibold
                           hover:bg-green-700 disabled:opacity-40 transition flex items-center gap-1"
              >
                <Check size={13} /> Bulk Approve ({selected.size})
              </button>
              <button
                onClick={() => setConfirmAction('reject')}
                disabled={selected.size === 0}
                className="px-4 py-1.5 bg-[#d32f2f] text-white rounded-xl text-xs font-semibold
                           hover:bg-red-800 disabled:opacity-40 transition flex items-center gap-1"
              >
                <X size={13} /> Bulk Reject ({selected.size})
              </button>
            </div>
          </div>

          <div className="overflow-x-auto rounded-lg border border-slate-200">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                  <th className="px-3 py-2.5 text-left">
                    <input type="checkbox" checked={selected.size === results.length && results.length > 0}
                      onChange={toggleAll} className="rounded" />
                  </th>
                  <th className="px-3 py-2.5 text-left">Policy No.</th>
                  <th className="px-3 py-2.5 text-left">Product</th>
                  <th className="px-3 py-2.5 text-left">UIN</th>
                  <th className="px-3 py-2.5 text-right">Core System</th>
                  <th className="px-3 py-2.5 text-right">Precision Pro</th>
                  <th className="px-3 py-2.5 text-right">Variance</th>
                  <th className="px-3 py-2.5 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {results.map(r => (
                  <tr key={r.caseId} className={`${varianceRowColor(r.variance)} hover:bg-slate-50 text-slate-700`}>
                    <td className="px-3 py-2.5">
                      <input type="checkbox" checked={selected.has(r.caseId)}
                        onChange={() => toggleSelect(r.caseId)} className="rounded" />
                    </td>
                    <td className="px-3 py-2.5 font-semibold">{r.policyNumber}</td>
                    <td className="px-3 py-2.5">{r.productName}</td>
                    <td className="px-3 py-2.5">{r.uin}</td>
                    <td className="px-3 py-2.5 text-right">₹ {INR(r.coreSystemAmount)}</td>
                    <td className="px-3 py-2.5 text-right">₹ {INR(r.precisionProAmount)}</td>
                    <td className="px-3 py-2.5 text-right font-semibold">₹ {INR(r.variance)}</td>
                    <td className="px-3 py-2.5 text-center"><StatusBadge status={r.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Confirm dialog */}
      {confirmAction && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className={`${CARD_CLS} p-6 w-full max-w-md space-y-4`}>
            <h3 className="text-lg font-bold text-[#004282]">
              Confirm Bulk {confirmAction === 'approve' ? 'Approve' : 'Reject'}
            </h3>
            <p className="text-sm text-slate-600">
              You are about to {confirmAction} <strong>{selected.size}</strong> case(s). This action cannot be undone.
            </p>
            <textarea
              value={bulkRemarks}
              onChange={e => setBulkRemarks(e.target.value)}
              className={`${INPUT_CLS} h-20`}
              placeholder="Optional remarks…"
            />
            <div className="flex gap-3 justify-end">
              <button onClick={() => setConfirmAction(null)}
                className="px-4 py-2 text-sm font-semibold text-slate-600 hover:text-slate-800 transition">
                Cancel
              </button>
              <button
                onClick={handleBulkDecision}
                disabled={deciding}
                className={`px-5 py-2 rounded-xl text-sm font-semibold text-white transition disabled:opacity-60 flex items-center gap-2
                  ${confirmAction === 'approve' ? 'bg-green-600 hover:bg-green-700' : 'bg-[#d32f2f] hover:bg-red-800'}`}
              >
                {deciding && <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />}
                {confirmAction === 'approve' ? 'Approve' : 'Reject'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Individual Policy Audit Table ───────────────────────────────────────────

export function IndividualAuditTable({ auditType }: { auditType: string }) {
  const [cases, setCases] = useState<AuditCase[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('All');
  const [filterOpen, setFilterOpen] = useState(false);

  const fetchCases = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await axios.get(`${API_URL}/api/audit/cases`, { params: { auditType, inputMode: 'Single' } });
      setCases(r.data);
    } catch {
      setError('Failed to load audit cases.');
    } finally {
      setLoading(false);
    }
  }, [auditType]);

  useEffect(() => { fetchCases(); }, [fetchCases]);

  const filtered = statusFilter === 'All' ? cases : cases.filter(c => c.status === statusFilter);

  if (loading) return <div className="flex items-center justify-center h-32"><Spinner size={32} /></div>;
  if (error) return <ErrBanner msg={error} />;

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-slate-400">{filtered.length} record(s)</p>
        <div className="relative">
          <button onClick={() => setFilterOpen(o => !o)}
            className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50 transition">
            <Filter size={13} /> {statusFilter} <ChevronDown size={13} />
          </button>
          {filterOpen && (
            <div className="absolute right-0 mt-1 bg-white border border-slate-200 rounded-lg shadow-lg z-10 py-1 w-32">
              {['All', 'Pending', 'Approved', 'Rejected'].map(s => (
                <button key={s} onClick={() => { setStatusFilter(s); setFilterOpen(false); }}
                  className={`block w-full text-left px-3 py-1.5 text-xs hover:bg-slate-50
                    ${statusFilter === s ? 'font-bold text-[#004282]' : 'text-slate-600'}`}>
                  {s}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {filtered.length === 0 ? (
        <p className="text-sm text-slate-400 text-center py-8">No records found.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                <th className="px-4 py-2.5 text-left">Policy No.</th>
                <th className="px-4 py-2.5 text-left">Product</th>
                <th className="px-4 py-2.5 text-left">Date Searched</th>
                <th className="px-4 py-2.5 text-right">Core Amount</th>
                <th className="px-4 py-2.5 text-right">PP Amount</th>
                <th className="px-4 py-2.5 text-right">Variance</th>
                <th className="px-4 py-2.5 text-center">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filtered.map(c => (
                <tr key={c.caseId} className={`${varianceRowColor(c.variance)} hover:bg-slate-50 text-slate-700`}>
                  <td className="px-4 py-2.5 font-semibold">{c.policyNumber}</td>
                  <td className="px-4 py-2.5">{c.productName}</td>
                  <td className="px-4 py-2.5">{c.dateSearched ? fmtDate(c.dateSearched) : '—'}</td>
                  <td className="px-4 py-2.5 text-right">₹ {INR(c.coreSystemAmount)}</td>
                  <td className="px-4 py-2.5 text-right">₹ {INR(c.precisionProAmount)}</td>
                  <td className="px-4 py-2.5 text-right font-semibold">₹ {INR(c.variance)}</td>
                  <td className="px-4 py-2.5 text-center"><StatusBadge status={c.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

// ─── Batch Audit Table ───────────────────────────────────────────────────────

export function BatchAuditTable({ auditType }: { auditType: string }) {
  const [batches, setBatches] = useState<BatchRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedBatch, setExpandedBatch] = useState<string | null>(null);
  const [batchCases, setBatchCases] = useState<AuditCase[]>([]);
  const [casesLoading, setCasesLoading] = useState(false);

  const fetchBatches = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await axios.get(`${API_URL}/api/audit/batches`, { params: { auditType } });
      setBatches(r.data);
    } catch {
      setError('Failed to load batch records.');
    } finally {
      setLoading(false);
    }
  }, [auditType]);

  useEffect(() => { fetchBatches(); }, [fetchBatches]);

  const handleExpand = async (batchId: string) => {
    if (expandedBatch === batchId) { setExpandedBatch(null); return; }
    setExpandedBatch(batchId);
    setCasesLoading(true);
    setBatchCases([]);
    try {
      const r = await axios.get(`${API_URL}/api/audit/batches/${batchId}/cases`);
      setBatchCases(r.data);
    } catch {
      setBatchCases([]);
    } finally {
      setCasesLoading(false);
    }
  };

  if (loading) return <div className="flex items-center justify-center h-32"><Spinner size={32} /></div>;
  if (error) return <ErrBanner msg={error} />;

  return (
    <div className="space-y-3">
      <p className="text-xs text-slate-400">{batches.length} batch(es)</p>

      {batches.length === 0 ? (
        <p className="text-sm text-slate-400 text-center py-8">No batch uploads found.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                <th className="px-4 py-2.5 text-left">Upload Date</th>
                <th className="px-4 py-2.5 text-left">File Name</th>
                <th className="px-4 py-2.5 text-left">Audit Type</th>
                <th className="px-4 py-2.5 text-right">Total</th>
                <th className="px-4 py-2.5 text-right">Processed</th>
                <th className="px-4 py-2.5 text-right">Pending</th>
                <th className="px-4 py-2.5 text-center">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {batches.map(b => (
                <tr key={b.batchId}
                  onClick={() => handleExpand(b.batchId)}
                  className="hover:bg-slate-50 text-slate-700 cursor-pointer">
                  <td className="px-4 py-2.5">{fmtDate(b.uploadDate)}</td>
                  <td className="px-4 py-2.5 font-semibold flex items-center gap-1">
                    <FileSpreadsheet size={14} className="text-[#007bff]" /> {b.fileName}
                  </td>
                  <td className="px-4 py-2.5">{b.auditType}</td>
                  <td className="px-4 py-2.5 text-right">{b.totalCases.toLocaleString('en-IN')}</td>
                  <td className="px-4 py-2.5 text-right">{b.processed.toLocaleString('en-IN')}</td>
                  <td className="px-4 py-2.5 text-right">{b.pending.toLocaleString('en-IN')}</td>
                  <td className="px-4 py-2.5 text-center"><StatusBadge status={b.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Expanded batch cases */}
      {expandedBatch && (
        <div className={`${CARD_CLS} p-5 space-y-3`}>
          <h4 className="text-sm font-semibold text-[#004282]">Cases in Batch</h4>
          {casesLoading ? (
            <div className="flex items-center justify-center h-20"><Spinner size={24} /></div>
          ) : batchCases.length === 0 ? (
            <p className="text-sm text-slate-400">No cases found for this batch.</p>
          ) : (
            <div className="overflow-x-auto rounded-lg border border-slate-200">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    <th className="px-3 py-2 text-left">Policy No.</th>
                    <th className="px-3 py-2 text-left">Product</th>
                    <th className="px-3 py-2 text-right">Core System</th>
                    <th className="px-3 py-2 text-right">Precision Pro</th>
                    <th className="px-3 py-2 text-right">Variance</th>
                    <th className="px-3 py-2 text-center">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {batchCases.map(c => (
                    <tr key={c.caseId} className={`${varianceRowColor(c.variance)} hover:bg-slate-50 text-slate-700`}>
                      <td className="px-3 py-2 font-semibold">{c.policyNumber}</td>
                      <td className="px-3 py-2">{c.productName}</td>
                      <td className="px-3 py-2 text-right">₹ {INR(c.coreSystemAmount)}</td>
                      <td className="px-3 py-2 text-right">₹ {INR(c.precisionProAmount)}</td>
                      <td className="px-3 py-2 text-right font-semibold">₹ {INR(c.variance)}</td>
                      <td className="px-3 py-2 text-center"><StatusBadge status={c.status} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// ─── Audit Logs Table ────────────────────────────────────────────────────────

export function AuditLogsTable() {
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await axios.get(`${API_URL}/api/audit/logs`);
      setLogs(r.data);
    } catch {
      setError('Failed to load audit logs.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchLogs(); }, [fetchLogs]);

  if (loading) return <div className="flex items-center justify-center h-32"><Spinner size={32} /></div>;
  if (error) return <ErrBanner msg={error} />;

  return (
    <div className="space-y-3">
      <p className="text-xs text-slate-400">{logs.length} log entry(ies)</p>

      {logs.length === 0 ? (
        <p className="text-sm text-slate-400 text-center py-8">No audit logs found.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                <th className="px-4 py-2.5 text-left">Log ID</th>
                <th className="px-4 py-2.5 text-left">Event Type</th>
                <th className="px-4 py-2.5 text-left">Case ID</th>
                <th className="px-4 py-2.5 text-left">Done By</th>
                <th className="px-4 py-2.5 text-left">Done At</th>
                <th className="px-4 py-2.5 text-left">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {logs.map(l => (
                <tr key={l.logId} className="hover:bg-slate-50 text-slate-700">
                  <td className="px-4 py-2.5 font-mono text-xs">{l.logId}</td>
                  <td className="px-4 py-2.5"><StatusBadge status={l.eventType} /></td>
                  <td className="px-4 py-2.5 font-semibold">{l.caseId}</td>
                  <td className="px-4 py-2.5">{l.doneBy}</td>
                  <td className="px-4 py-2.5">{fmtDate(l.doneAt)}</td>
                  <td className="px-4 py-2.5 text-slate-500 max-w-xs truncate">{l.details}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

// ─── Main export ─────────────────────────────────────────────────────────────

export type AuditSubModule = 'payout-verification' | 'addition-bonus';
export type AuditSubOption = 'single' | 'excel';

export default function AuditModule({ sub, subOption = 'single' }: { sub: AuditSubModule; subOption?: AuditSubOption }): JSX.Element {
  const [activeTab, setActiveTab] = useState<AuditSubOption>(subOption);

  // Sync if prop changes externally
  useEffect(() => { setActiveTab(subOption); }, [subOption]);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          {subTitle(sub)}
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          {sub === 'payout-verification'
            ? 'Verify policy payouts between Core System and Precision Pro.'
            : 'Audit addition and bonus amounts across systems.'}
        </p>
      </div>

      {/* Tab switcher */}
      <TabSwitcher active={activeTab} onChange={setActiveTab} />

      {/* Active sub-option */}
      {activeTab === 'single' && <SinglePolicy sub={sub} />}
      {activeTab === 'excel' && <ExcelUpload sub={sub} />}
    </div>
  );
}

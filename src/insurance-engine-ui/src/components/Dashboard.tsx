import { useState, useEffect } from 'react';
import {
  TrendingUp, BarChart3, ClipboardCheck, CheckCircle2,
  ShieldCheck, PlusCircle, Settings,
  AlertCircle, Clock, Download,
} from 'lucide-react';
import axios from 'axios';
import { getBatches, type UploadBatch } from '../api';

// ─── Constants & helpers ─────────────────────────────────────────────────────

const API_URL = import.meta.env.VITE_API_URL || 'http://ezytek1706-003-site3.rtempurl.com';
const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 });
const CARD_CLS = 'bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)]';

const fmtDate = (d: string) =>
  new Date(d).toLocaleString('en-IN', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });

type BatchStatus = 'Processed' | 'In Progress' | 'Failed';

interface BatchRow {
  id: number;
  date: string;
  reportType: string;
  requestedBy: string;
  status: BatchStatus;
  fileName: string;
  totalRows: number;
}

interface AuditDashboard {
  totalThisMonth: number;
  approvedCount: number;
  rejectedCount: number;
  pendingCount: number;
  totalVariance: number;
}

interface AuditCaseRow {
  caseId: string;
  policyNumber: string;
  auditType: string;
  coreSystemAmount: number;
  precisionProAmount: number;
  variance: number;
  status: string;
  dateSearched?: string;
}

// ─── Status badge styles ─────────────────────────────────────────────────────

const STATUS_STYLES: Record<BatchStatus, { cls: string; icon: React.ReactNode }> = {
  Processed:     { cls: 'bg-green-50 text-green-700 border border-green-200', icon: <CheckCircle2 size={12} /> },
  'In Progress': { cls: 'bg-amber-50 text-amber-700 border border-amber-200', icon: <Clock size={12} /> },
  Failed:        { cls: 'bg-red-50 text-red-700 border border-red-200',       icon: <AlertCircle size={12} /> },
};

const AUDIT_STATUS_CLS: Record<string, string> = {
  Approved: 'bg-green-50 text-green-700 border border-green-200',
  Rejected: 'bg-red-50 text-red-700 border border-red-200',
  Pending:  'bg-amber-50 text-amber-700 border border-amber-200',
};

function toStatus(b: UploadBatch): BatchStatus {
  if (b.processedRows === 0 && b.totalRows > 0) return 'Failed';
  if (b.processedRows < b.totalRows) return 'In Progress';
  return 'Processed';
}

// ─── Module shortcut definitions ─────────────────────────────────────────────

const MODULE_CARDS = [
  { icon: TrendingUp,  label: 'Benefit Illustration',       desc: 'Generate benefit illustrations for Traditional & ULIP products' },
  { icon: BarChart3,   label: 'YPYG',                       desc: 'You Pay You Get calculations' },
  { icon: ShieldCheck, label: 'Audit — Payout Verification', desc: 'Verify payout amounts against core system' },
  { icon: PlusCircle,  label: 'Audit — Addition / Bonus',    desc: 'Verify additions and bonus calculations' },
  { icon: Settings,    label: 'Admin Master',                desc: 'Manage products, formulas, users, and system configuration' },
] as const;

// ─── Main component ─────────────────────────────────────────────────────────

export default function Dashboard() {
  const [batches, setBatches] = useState<UploadBatch[]>([]);
  const [auditDash, setAuditDash] = useState<AuditDashboard | null>(null);
  const [auditCases, setAuditCases] = useState<AuditCaseRow[]>([]);
  const [loadingBatches, setLoadingBatches] = useState(true);
  const [loadingAudit, setLoadingAudit] = useState(true);

  useEffect(() => {
    getBatches()
      .then(r => setBatches(r.data))
      .catch(() => {})
      .finally(() => setLoadingBatches(false));

    axios.get<AuditDashboard>(`${API_URL}/api/audit/dashboard`)
      .then(r => setAuditDash(r.data))
      .catch(() => {});

    axios.get<AuditCaseRow[]>(`${API_URL}/api/audit/cases`, { params: { page: 1, pageSize: 5 } })
      .then(r => setAuditCases(r.data))
      .catch(() => {})
      .finally(() => setLoadingAudit(false));
  }, []);

  const biBatchCount   = batches.filter(b => b.uploadType.toLowerCase().includes('bi')).length;
  const ypygBatchCount = batches.filter(b => !b.uploadType.toLowerCase().includes('bi')).length;

  const batchRows: BatchRow[] = batches.map(b => ({
    id: b.id,
    date: b.completedAt || b.createdAt || '',
    reportType: b.uploadType,
    requestedBy: 'admin',
    status: toStatus(b),
    fileName: b.fileName,
    totalRows: b.totalRows,
  }));

  return (
    <div className="space-y-8">
      {/* ── Page heading ───────────────────────────────────────────────── */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Dashboard
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">Welcome to PrecisionPro — your insurance calculation hub.</p>
      </div>

      {/* ── Quick stats cards ──────────────────────────────────────────── */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <StatCard
          icon={<TrendingUp size={22} className="text-[#004282]" />}
          label="Total BI Generated"
          value={INR(biBatchCount)}
          sub="benefit illustration batches"
          accent="blue"
        />
        <StatCard
          icon={<BarChart3 size={22} className="text-[#004282]" />}
          label="Total YPYG Calculated"
          value={INR(ypygBatchCount)}
          sub="YPYG batches processed"
          accent="indigo"
        />
        <StatCard
          icon={<ClipboardCheck size={22} className="text-[#004282]" />}
          label="Audit Cases Pending"
          value={auditDash ? INR(auditDash.pendingCount) : '—'}
          sub="cases awaiting review"
          accent="amber"
        />
        <StatCard
          icon={<CheckCircle2 size={22} className="text-[#004282]" />}
          label="Audit Approvals"
          value={auditDash ? INR(auditDash.approvedCount) : '—'}
          sub="cases approved this month"
          accent="green"
        />
      </div>

      {/* ── Module shortcut cards ──────────────────────────────────────── */}
      <div>
        <h3 className="text-base font-bold text-[#004282] mb-4">
          Modules
          <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
        </h3>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-5">
          {MODULE_CARDS.map(({ icon: Icon, label, desc }) => (
            <div
              key={label}
              className={`${CARD_CLS} p-5 border border-slate-100 hover:border-[#007bff]/40 hover:shadow-lg transition-all cursor-default`}
            >
              <Icon size={28} className="text-[#004282] mb-3" />
              <p className="text-sm font-bold text-[#004282]">{label}</p>
              <p className="text-xs text-slate-500 mt-1 leading-relaxed">{desc}</p>
            </div>
          ))}
        </div>
      </div>

      {/* ── Recent Activity ────────────────────────────────────────────── */}
      <div className="grid lg:grid-cols-2 gap-6">
        {/* Batch Activity */}
        <div className={`${CARD_CLS} overflow-hidden`}>
          <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
            <BarChart3 size={18} className="text-[#007bff]" />
            <h3 className="text-base font-bold text-[#004282]">
              Batch Activity
              <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
            </h3>
          </div>

          {loadingBatches && <Spinner />}

          {!loadingBatches && batchRows.length === 0 && (
            <EmptyState text="No batch activity yet. Upload an Excel file to get started." />
          )}

          {!loadingBatches && batchRows.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-blue-50/60 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    <th className="px-5 py-3 text-left">Date</th>
                    <th className="px-5 py-3 text-left">Report Type</th>
                    <th className="px-5 py-3 text-left">Requested By</th>
                    <th className="px-5 py-3 text-center">Status</th>
                    <th className="px-5 py-3 text-right">Rows</th>
                    <th className="px-5 py-3 text-center">Download</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {batchRows.map(row => {
                    const s = STATUS_STYLES[row.status];
                    return (
                      <tr key={row.id} className="hover:bg-slate-50 transition-colors text-slate-700">
                        <td className="px-5 py-3 text-xs text-slate-400">{fmtDate(row.date)}</td>
                        <td className="px-5 py-3 font-medium">{row.reportType}</td>
                        <td className="px-5 py-3">{row.requestedBy}</td>
                        <td className="px-5 py-3">
                          <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold ${s.cls}`}>
                            {s.icon}
                            {row.status}
                          </span>
                        </td>
                        <td className="px-5 py-3 text-right font-mono">{row.totalRows}</td>
                        <td className="px-5 py-3 text-center">
                          {row.status === 'Processed' ? (
                            <button
                              className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs
                                         bg-blue-50 text-[#004282] border border-blue-200 hover:bg-blue-100 transition"
                              title={`Download ${row.fileName}`}
                            >
                              <Download size={12} />
                              Export
                            </button>
                          ) : (
                            <span className="text-slate-300">—</span>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Recent Audit Cases */}
        <div className={`${CARD_CLS} overflow-hidden`}>
          <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
            <ShieldCheck size={18} className="text-[#007bff]" />
            <h3 className="text-base font-bold text-[#004282]">
              Recent Audit Cases
              <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
            </h3>
          </div>

          {loadingAudit && <Spinner />}

          {!loadingAudit && auditCases.length === 0 && (
            <EmptyState text="No audit cases yet. Search or upload cases to begin." />
          )}

          {!loadingAudit && auditCases.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-blue-50/60 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    <th className="px-4 py-3 text-left">Policy #</th>
                    <th className="px-4 py-3 text-left">Audit Type</th>
                    <th className="px-4 py-3 text-right">Core Amt</th>
                    <th className="px-4 py-3 text-right">PP Amt</th>
                    <th className="px-4 py-3 text-right">Variance</th>
                    <th className="px-4 py-3 text-center">Status</th>
                    <th className="px-4 py-3 text-left">Date</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {auditCases.map(c => (
                    <tr key={c.caseId} className="hover:bg-slate-50 transition-colors text-slate-700">
                      <td className="px-4 py-3 font-medium">{c.policyNumber}</td>
                      <td className="px-4 py-3 text-xs">{c.auditType}</td>
                      <td className="px-4 py-3 text-right font-mono text-xs">{INR(c.coreSystemAmount)}</td>
                      <td className="px-4 py-3 text-right font-mono text-xs">{INR(c.precisionProAmount)}</td>
                      <td className="px-4 py-3 text-right font-mono text-xs">{INR(c.variance)}</td>
                      <td className="px-4 py-3 text-center">
                        <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold ${AUDIT_STATUS_CLS[c.status] ?? 'bg-slate-50 text-slate-600 border border-slate-200'}`}>
                          {c.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-xs text-slate-400">{c.dateSearched ? fmtDate(c.dateSearched) : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Shared small components ─────────────────────────────────────────────────

function Spinner() {
  return (
    <div className="flex justify-center py-16">
      <span className="w-8 h-8 border-2 border-[#007bff]/20 border-t-[#007bff] rounded-full animate-spin" />
    </div>
  );
}

function EmptyState({ text }: { text: string }) {
  return (
    <div className="py-16 text-center text-slate-400 text-sm">{text}</div>
  );
}

function StatCard({
  icon, label, value, sub, accent,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub: string;
  accent: 'blue' | 'indigo' | 'amber' | 'green';
}) {
  const borderMap = {
    blue: 'border-blue-100',
    indigo: 'border-indigo-100',
    amber: 'border-amber-100',
    green: 'border-green-100',
  };
  return (
    <div className={`bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5 border-t-4 ${borderMap[accent]}`}>
      <div className="flex items-center gap-2 mb-3">
        {icon}
        <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{label}</p>
      </div>
      <p className="text-3xl font-extrabold text-[#004282]">{value}</p>
      <p className="text-xs text-slate-400 mt-1">{sub}</p>
    </div>
  );
}

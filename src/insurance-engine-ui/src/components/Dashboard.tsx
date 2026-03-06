import { useState, useEffect } from 'react';
import { BarChart3, FileUp, CheckCircle2, AlertCircle, Clock, Download } from 'lucide-react';
import { getBatches, type UploadBatch } from '../api';

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 });

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

const STATUS_STYLES: Record<BatchStatus, { cls: string; icon: React.ReactNode }> = {
  Processed: { cls: 'bg-green-50 text-green-700 border border-green-200', icon: <CheckCircle2 size={12} /> },
  'In Progress': { cls: 'bg-amber-50 text-amber-700 border border-amber-200', icon: <Clock size={12} /> },
  Failed: { cls: 'bg-red-50 text-red-700 border border-red-200', icon: <AlertCircle size={12} /> },
};

function toStatus(b: UploadBatch): BatchStatus {
  if (b.processedRows === 0 && b.totalRows > 0) return 'Failed';
  if (b.processedRows < b.totalRows) return 'In Progress';
  return 'Processed';
}

export default function Dashboard() {
  const [batches, setBatches] = useState<UploadBatch[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getBatches()
      .then(r => setBatches(r.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const rows: BatchRow[] = batches.map(b => ({
    id: b.id,
    date: b.uploadedAt,
    reportType: b.uploadType,
    requestedBy: 'admin',
    status: toStatus(b),
    fileName: b.fileName,
    totalRows: b.totalRows,
  }));

  const totalCalculations = batches.reduce((s, b) => s + b.processedRows, 0);
  const totalUploads = batches.length;
  const totalErrors = batches.reduce((s, b) => s + b.errorRows, 0);

  return (
    <div className="space-y-8">
      {/* Page heading */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Dashboard
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">Welcome to PrecisionPro — your insurance calculation hub.</p>
      </div>

      {/* Summary cards */}
      <div className="grid sm:grid-cols-3 gap-5">
        <StatCard
          icon={<BarChart3 size={22} className="text-[#004282]" />}
          label="Recent Calculations"
          value={INR(totalCalculations)}
          sub="rows processed across all batches"
          accent="blue"
        />
        <StatCard
          icon={<FileUp size={22} className="text-[#007bff]" />}
          label="Excel Uploads Processed"
          value={INR(totalUploads)}
          sub="upload batches submitted"
          accent="indigo"
        />
        <StatCard
          icon={<AlertCircle size={22} className="text-[#d32f2f]" />}
          label="Total Errors"
          value={INR(totalErrors)}
          sub="row errors in uploaded files"
          accent={totalErrors > 0 ? 'red' : 'blue'}
        />
      </div>

      {/* Batch Activity table */}
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
          <BarChart3 size={18} className="text-[#007bff]" />
          <h3 className="text-base font-bold text-[#004282]">
            Batch Activity
            <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
          </h3>
        </div>

        {loading && (
          <div className="flex justify-center py-16">
            <span className="w-8 h-8 border-2 border-[#007bff]/20 border-t-[#007bff] rounded-full animate-spin" />
          </div>
        )}

        {!loading && rows.length === 0 && (
          <div className="py-16 text-center text-slate-400 text-sm">
            No batch activity yet. Upload an Excel file to get started.
          </div>
        )}

        {!loading && rows.length > 0 && (
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
                {rows.map(row => {
                  const s = STATUS_STYLES[row.status];
                  return (
                    <tr key={row.id} className="hover:bg-slate-50 transition-colors text-slate-700">
                      <td className="px-5 py-3 text-xs text-slate-400">
                        {new Date(row.date).toLocaleString('en-IN', {
                          day: '2-digit', month: 'short', year: 'numeric',
                          hour: '2-digit', minute: '2-digit',
                        })}
                      </td>
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
    </div>
  );
}

function StatCard({
  icon, label, value, sub, accent,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub: string;
  accent: 'blue' | 'indigo' | 'red';
}) {
  const borderMap = { blue: 'border-blue-100', indigo: 'border-indigo-100', red: 'border-red-100' };
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

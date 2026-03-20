import { useState, useEffect, useCallback } from 'react';
import { ClipboardList, RefreshCw, Upload, BarChart3, AlertCircle, CheckCircle2 } from 'lucide-react';
import { getBatches, type UploadBatch } from '../api';

type EventType = 'upload_success' | 'upload_error' | 'batch_info';

interface AuditEvent {
  id: string;
  type: EventType;
  module: string;
  summary: string;
  detail: string;
  timestamp: string;
}

function batchesToEvents(batches: UploadBatch[]): AuditEvent[] {
  return batches.map(b => ({
    id: `batch-${b.id}`,
    type:
      b.errorRows === 0
        ? 'upload_success'
        : b.processedRows === 0
          ? 'upload_error'
          : 'batch_info',
    module: 'Bulk Upload',
    summary: `${b.uploadType} upload — ${b.fileName}`,
    detail: `Total: ${b.totalRows} rows · Processed: ${b.processedRows} · Errors: ${b.errorRows}`,
    timestamp: b.completedAt || b.createdAt || '',
  }));
}

const EVENT_STYLES: Record<
  EventType,
  { icon: React.ReactNode; bgColor: string; textColor: string; borderColor: string }
> = {
  upload_success: {
    icon: <CheckCircle2 size={16} />,
    bgColor: 'bg-green-50',
    textColor: 'text-green-600',
    borderColor: 'border-green-200',
  },
  upload_error: {
    icon: <AlertCircle size={16} />,
    bgColor: 'bg-red-50',
    textColor: 'text-[#d32f2f]',
    borderColor: 'border-red-200',
  },
  batch_info: {
    icon: <Upload size={16} />,
    bgColor: 'bg-amber-50',
    textColor: 'text-amber-600',
    borderColor: 'border-amber-200',
  },
};

export default function AuditLog() {
  const [batches, setBatches] = useState<UploadBatch[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await getBatches();
      setBatches(r.data);
    } catch {
      setError('Could not load activity log. Ensure the API is running.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const events = batchesToEvents(batches);

  // Summary counters
  const totalUploads = batches.length;
  const totalRows = batches.reduce((s, b) => s + b.totalRows, 0);
  const totalErrors = batches.reduce((s, b) => s + b.errorRows, 0);

  return (
    <div className="space-y-8">
      {/* Heading */}
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h2 className="text-2xl font-bold text-[#004282]">
            Audit Log
            <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
          </h2>
          <p className="mt-2 text-slate-500 text-sm">
            System activity timeline — upload batches and change events.
          </p>
        </div>
        <button
          onClick={fetchData}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-2 rounded-full border border-[#004282] text-[#004282]
                     text-sm font-semibold hover:bg-blue-50 disabled:opacity-50 transition-colors"
        >
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
          Refresh
        </button>
      </div>

      {/* Summary metric cards */}
      <div className="grid sm:grid-cols-3 gap-4">
        {[
          {
            icon: <Upload size={18} className="text-[#007bff]" />,
            label: 'Total Upload Batches',
            value: totalUploads,
            red: false,
          },
          {
            icon: <BarChart3 size={18} className="text-[#007bff]" />,
            label: 'Total Rows Processed',
            value: totalRows,
            red: false,
          },
          {
            icon: <AlertCircle size={18} className="text-[#d32f2f]" />,
            label: 'Total Row Errors',
            value: totalErrors,
            red: true,
          },
        ].map(card => (
          <div key={card.label} className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-5">
            <div className="flex items-center gap-2 mb-2">
              {card.icon}
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{card.label}</p>
            </div>
            <p className={`text-3xl font-extrabold ${card.red && card.value > 0 ? 'text-[#d32f2f]' : 'text-[#004282]'}`}>
              {card.value.toLocaleString('en-IN')}
            </p>
          </div>
        ))}
      </div>

      {/* Activity timeline */}
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-3">
          <ClipboardList size={18} className="text-[#007bff]" />
          <h3 className="text-base font-bold text-[#004282]">
            Activity Timeline
            <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
          </h3>
        </div>

        {loading && (
          <div className="flex items-center justify-center py-16">
            <span className="inline-block w-8 h-8 border-2 border-[#007bff]/20 border-t-[#007bff] rounded-full animate-spin" />
          </div>
        )}

        {!loading && error && (
          <div className="mx-6 my-6 flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700">
            <AlertCircle size={16} className="mt-0.5 flex-shrink-0 text-[#d32f2f]" />
            {error}
          </div>
        )}

        {!loading && !error && events.length === 0 && (
          <div className="px-6 py-16 text-center text-slate-400 text-sm">
            No activity recorded yet. Events will appear here after uploads and calculations.
          </div>
        )}

        {!loading && !error && events.length > 0 && (
          <div className="divide-y divide-slate-100">
            {/* Table header */}
            <div className="hidden sm:grid grid-cols-[auto_1fr_auto] gap-6 px-6 py-2 bg-blue-50/50">
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Status</span>
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Event</span>
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Time</span>
            </div>
            {events.map(ev => {
              const style = EVENT_STYLES[ev.type];
              return (
                <div key={ev.id} className="px-6 py-4 hover:bg-slate-50 transition-colors">
                  <div className="flex items-start gap-4">
                    {/* Status badge */}
                    <div
                      className={`flex-shrink-0 w-8 h-8 rounded-lg flex items-center justify-center
                        ${style.bgColor} ${style.textColor} border ${style.borderColor}`}
                    >
                      {style.icon}
                    </div>

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <p className="text-sm font-semibold text-slate-700">{ev.summary}</p>
                        <span className="px-2 py-0.5 bg-blue-50 text-[#007bff] rounded-full text-xs font-medium">
                          {ev.module}
                        </span>
                      </div>
                      <p className="text-xs text-slate-400 mt-0.5">{ev.detail}</p>
                    </div>

                    {/* Timestamp */}
                    <div className="flex-shrink-0 text-xs text-slate-400 text-right">
                      {ev.timestamp
                        ? new Date(ev.timestamp).toLocaleString('en-IN', {
                            day: '2-digit',
                            month: 'short',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit',
                          })
                        : '—'}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Upload Batches detail table */}
      {!loading && !error && batches.length > 0 && (
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100">
            <h3 className="text-base font-bold text-[#004282]">
              Upload Batch Details
              <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
            </h3>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                  <th className="px-6 py-3 text-left">Batch ID</th>
                  <th className="px-6 py-3 text-left">File</th>
                  <th className="px-6 py-3 text-left">Type</th>
                  <th className="px-6 py-3 text-right">Total</th>
                  <th className="px-6 py-3 text-right">Processed</th>
                  <th className="px-6 py-3 text-right">Errors</th>
                  <th className="px-6 py-3 text-left">Uploaded At</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {batches.map(b => (
                  <tr key={b.id} className="hover:bg-slate-50 transition-colors text-slate-700">
                    <td className="px-6 py-3 font-mono text-xs text-slate-400">#{b.id}</td>
                    <td className="px-6 py-3 font-medium">{b.fileName}</td>
                    <td className="px-6 py-3">
                      <span className="px-2 py-0.5 bg-blue-100 text-[#007bff] rounded-full text-xs font-medium">
                        {b.uploadType}
                      </span>
                    </td>
                    <td className="px-6 py-3 text-right">{b.totalRows}</td>
                    <td className="px-6 py-3 text-right text-green-600 font-medium">{b.processedRows}</td>
                    <td className="px-6 py-3 text-right">
                      <span className={b.errorRows > 0 ? 'text-[#d32f2f] font-bold' : 'text-slate-400'}>
                        {b.errorRows}
                      </span>
                    </td>
                    <td className="px-6 py-3 text-xs text-slate-400">
                      {(b.completedAt || b.createdAt)
                        ? new Date(b.completedAt || b.createdAt!).toLocaleString('en-IN', {
                            day: '2-digit',
                            month: 'short',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit',
                          })
                        : '—'}
                    </td>
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

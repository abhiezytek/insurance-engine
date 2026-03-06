import { useState, useRef, useCallback, useEffect } from 'react';
import { CloudUpload, Download, FileSpreadsheet, CheckCircle, XCircle, Clock } from 'lucide-react';
import { uploadFile, getBatches, type UploadBatch } from '../api';

// ---------- Template data ----------
const FORMULA_TEMPLATE_CSV = `Name,Expression,ExecutionOrder,Description
GMB,AP * 11.5,1,Guaranteed Maturity Benefit
GSV,GMB * 0.30,2,Guaranteed Surrender Value
SSV,AP * 12,3,Special Surrender Value
MATURITY_BENEFIT,GMB,4,Maturity Benefit
DEATH_BENEFIT,"MAX(10*AP, 1.05*TotalPremiumPaid, SurrenderValue)",5,Death Benefit
`;

const PARAMETER_TEMPLATE_CSV = `Name,DataType,IsRequired,Description
AP,decimal,true,Annual Premium (excl. taxes and rider premium)
SA,decimal,true,Sum Assured
PPT,int,true,Premium Payment Term
PT,int,true,Policy Term
Age,int,true,Age at entry
TotalPremiumPaid,decimal,true,Total Premiums Paid
SurrenderValue,decimal,true,Current Surrender Value
`;

function downloadCsv(filename: string, content: string) {
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

// ---------- Component ----------
export default function Upload() {
  const [file, setFile] = useState<File | null>(null);
  const [uploadType, setUploadType] = useState<'Formulas' | 'Parameters'>('Formulas');
  const [productVersionId, setProductVersionId] = useState('1');
  const [result, setResult] = useState<{ totalRows: number; processedRows: number; errorRows: number } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [batches, setBatches] = useState<UploadBatch[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    getBatches().then(r => setBatches(r.data)).catch(err => console.warn('Failed to load batches:', err));
  }, []);

  const onDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragging(false);
    const dropped = e.dataTransfer.files[0];
    if (dropped) setFile(dropped);
  }, []);

  const handleUpload = async () => {
    if (!file) return;
    setLoading(true); setError(null); setResult(null);
    try {
      const resp = await uploadFile(file, uploadType, parseInt(productVersionId));
      setResult(resp.data);
      // Refresh batch list
      getBatches().then(r => setBatches(r.data)).catch(err => console.warn('Failed to refresh batches:', err));
    } catch (e: any) {
      setError(e.response?.data?.error || e.message || 'Upload failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-8">
      {/* Page heading */}
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Bulk Upload
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">
          Upload an <code className="bg-slate-100 px-1 rounded">.xlsx</code> or{' '}
          <code className="bg-slate-100 px-1 rounded">.csv</code> file to bulk-create parameters or formulas.
        </p>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        {/* Left: Upload form */}
        <div className="lg:col-span-2 space-y-6">
          {/* Template download cards */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-4">
              Download Sample Templates
            </h3>
            <div className="grid sm:grid-cols-2 gap-4">
              <button
                onClick={() => downloadCsv('formulas-template.csv', FORMULA_TEMPLATE_CSV)}
                className="flex items-center gap-3 p-4 border-2 border-dashed border-[#007bff] rounded-xl
                           hover:bg-blue-50 transition-colors text-left group"
              >
                <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center flex-shrink-0">
                  <FileSpreadsheet size={20} className="text-[#007bff]" />
                </div>
                <div>
                  <p className="font-semibold text-[#004282] text-sm group-hover:text-[#007bff]">Formulas Template</p>
                  <p className="text-xs text-slate-400 flex items-center gap-1 mt-0.5">
                    <Download size={11} /> formulas-template.csv
                  </p>
                </div>
              </button>

              <button
                onClick={() => downloadCsv('parameters-template.csv', PARAMETER_TEMPLATE_CSV)}
                className="flex items-center gap-3 p-4 border-2 border-dashed border-[#007bff] rounded-xl
                           hover:bg-blue-50 transition-colors text-left group"
              >
                <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center flex-shrink-0">
                  <FileSpreadsheet size={20} className="text-[#007bff]" />
                </div>
                <div>
                  <p className="font-semibold text-[#004282] text-sm group-hover:text-[#007bff]">Parameters Template</p>
                  <p className="text-xs text-slate-400 flex items-center gap-1 mt-0.5">
                    <Download size={11} /> parameters-template.csv
                  </p>
                </div>
              </button>
            </div>
          </div>

          {/* Upload form */}
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-5">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">Upload File</h3>

            {/* Drag-drop zone */}
            <div
              onDragOver={e => { e.preventDefault(); setDragging(true); }}
              onDragLeave={() => setDragging(false)}
              onDrop={onDrop}
              onClick={() => fileInputRef.current?.click()}
              className={`
                relative border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all
                ${dragging ? 'border-[#007bff] bg-blue-50' : 'border-slate-200 hover:border-[#007bff] hover:bg-blue-50/40'}
              `}
            >
              <CloudUpload size={36} className={`mx-auto mb-3 ${dragging ? 'text-[#007bff]' : 'text-slate-300'}`} />
              {file ? (
                <div>
                  <p className="font-semibold text-[#004282]">{file.name}</p>
                  <p className="text-xs text-slate-400 mt-1">{(file.size / 1024).toFixed(1)} KB · Click to change</p>
                </div>
              ) : (
                <div>
                  <p className="font-medium text-slate-600">Drag & drop your file here</p>
                  <p className="text-xs text-slate-400 mt-1">or <span className="text-[#007bff] underline">browse</span> · .xlsx or .csv</p>
                </div>
              )}
              <input
                ref={fileInputRef}
                type="file"
                accept=".xlsx,.csv"
                className="hidden"
                onChange={e => setFile(e.target.files?.[0] || null)}
              />
            </div>

            {/* Controls row */}
            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Upload Type</label>
                <select
                  value={uploadType}
                  onChange={e => setUploadType(e.target.value as 'Formulas' | 'Parameters')}
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                             focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]"
                >
                  <option>Formulas</option>
                  <option>Parameters</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Product Version ID</label>
                <input
                  type="number"
                  value={productVersionId}
                  onChange={e => setProductVersionId(e.target.value)}
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                             focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]"
                />
              </div>
            </div>

            <button
              onClick={handleUpload}
              disabled={!file || loading}
              className="w-full py-3 px-6 bg-[#004282] text-white rounded-xl font-semibold text-sm
                         hover:bg-[#003370] disabled:opacity-50 disabled:cursor-not-allowed
                         transition-colors flex items-center justify-center gap-2"
            >
              {loading ? (
                <>
                  <span className="inline-block w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  Uploading...
                </>
              ) : (
                <>
                  <CloudUpload size={16} />
                  Upload File
                </>
              )}
            </button>

            {/* Result / Error */}
            {error && (
              <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-xl text-sm text-red-700">
                <XCircle size={16} className="mt-0.5 flex-shrink-0 text-[#d32f2f]" />
                {error}
              </div>
            )}
            {result && (
              <div className="flex items-start gap-3 p-4 bg-green-50 border border-green-200 rounded-xl text-sm text-green-700">
                <CheckCircle size={16} className="mt-0.5 flex-shrink-0 text-green-600" />
                <span>
                  Upload complete — Total: <strong>{result.totalRows}</strong>, Processed:{' '}
                  <strong>{result.processedRows}</strong>, Errors:{' '}
                  <strong className={result.errorRows > 0 ? 'text-[#d32f2f]' : ''}>{result.errorRows}</strong>
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Right: Format guide */}
        <div className="space-y-6">
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-3">
              Formulas CSV Format
            </h3>
            <pre className="text-xs bg-slate-50 rounded-lg p-3 overflow-x-auto text-slate-700 leading-relaxed">
{`Name,Expression,ExecutionOrder
GMB,AP * 11.5,1
GSV,GMB * 0.30,2`}
            </pre>
          </div>
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-3">
              Parameters CSV Format
            </h3>
            <pre className="text-xs bg-slate-50 rounded-lg p-3 overflow-x-auto text-slate-700 leading-relaxed">
{`Name,DataType,IsRequired
AP,decimal,true
SA,decimal,true
PPT,int,true`}
            </pre>
          </div>
        </div>
      </div>

      {/* Batch Activity Table */}
      <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
          <h3 className="text-base font-bold text-[#004282]">
            Batch Activity
            <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
          </h3>
          <Clock size={16} className="text-slate-400" />
        </div>
        {batches.length === 0 ? (
          <div className="px-6 py-10 text-center text-slate-400 text-sm">No upload batches yet.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                  <th className="px-6 py-3 text-left">ID</th>
                  <th className="px-6 py-3 text-left">File</th>
                  <th className="px-6 py-3 text-left">Type</th>
                  <th className="px-6 py-3 text-right">Total</th>
                  <th className="px-6 py-3 text-right">Processed</th>
                  <th className="px-6 py-3 text-right">Errors</th>
                  <th className="px-6 py-3 text-left">Uploaded</th>
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
                      {b.uploadedAt ? new Date(b.uploadedAt).toLocaleString('en-IN') : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

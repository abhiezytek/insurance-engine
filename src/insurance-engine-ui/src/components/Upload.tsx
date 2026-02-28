import { useState } from 'react';
import { uploadFile } from '../api';

export default function Upload() {
  const [file, setFile] = useState<File | null>(null);
  const [uploadType, setUploadType] = useState('Formulas');
  const [productVersionId, setProductVersionId] = useState('1');
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleUpload = async () => {
    if (!file) return;
    setLoading(true); setError(null); setResult(null);
    try {
      const resp = await uploadFile(file, uploadType, parseInt(productVersionId));
      setResult(resp.data);
    } catch (e: any) {
      setError(e.response?.data?.error || e.message || 'Upload failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h2>Bulk Upload (Excel / CSV)</h2>
      <p style={{ color: '#555' }}>Upload an <code>.xlsx</code> or <code>.csv</code> file to bulk-create parameters or formulas.</p>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12, maxWidth: 400 }}>
        <div>
          <label style={{ fontWeight: 600 }}>Upload Type</label>
          <select value={uploadType} onChange={e => setUploadType(e.target.value)}
            style={{ display: 'block', marginTop: 4, padding: '6px 10px', width: '100%' }}>
            <option>Formulas</option>
            <option>Parameters</option>
          </select>
        </div>
        <div>
          <label style={{ fontWeight: 600 }}>Product Version ID</label>
          <input type="number" value={productVersionId} onChange={e => setProductVersionId(e.target.value)}
            style={{ display: 'block', marginTop: 4, padding: '6px 10px', width: '100%' }} />
        </div>
        <div>
          <label style={{ fontWeight: 600 }}>File (.xlsx or .csv)</label>
          <input type="file" accept=".xlsx,.csv" onChange={e => setFile(e.target.files?.[0] || null)}
            style={{ display: 'block', marginTop: 4 }} />
        </div>
        <button onClick={handleUpload} disabled={!file || loading}
          style={{ padding: '10px 24px', background: '#1a237e', color: 'white', border: 'none', borderRadius: 4, cursor: 'pointer', fontWeight: 600 }}>
          {loading ? 'Uploading...' : 'Upload'}
        </button>
      </div>
      {error && <div style={{ marginTop: 16, color: '#c62828', background: '#ffebee', padding: 12, borderRadius: 4 }}>{error}</div>}
      {result && (
        <div style={{ marginTop: 16, background: '#e8f5e9', padding: 12, borderRadius: 4 }}>
          <strong>Upload complete!</strong><br />
          Total: {result.totalRows}, Processed: {result.processedRows}, Errors: {result.errorRows}
        </div>
      )}
      <div style={{ marginTop: 24, background: '#f5f5f5', padding: 16, borderRadius: 4 }}>
        <h4 style={{ margin: '0 0 8px' }}>Expected CSV format for Formulas:</h4>
        <pre style={{ margin: 0, fontSize: 13 }}>Name,Expression,ExecutionOrder,Description
GMB,AP * 11.5,1,Guaranteed Maturity Benefit
GSV,GMB * 0.30,2,Guaranteed Surrender Value</pre>
        <h4 style={{ margin: '12px 0 8px' }}>Expected CSV format for Parameters:</h4>
        <pre style={{ margin: 0, fontSize: 13 }}>Name,DataType,Description
AP,decimal,Annual Premium
SA,decimal,Sum Assured</pre>
      </div>
    </div>
  );
}

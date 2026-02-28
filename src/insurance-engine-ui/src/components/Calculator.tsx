import { useState, useEffect } from 'react';
import { getProducts, runCalculation } from '../api';
import type { Product, CalculationResult } from '../api';

const DEFAULT_PARAMS: Record<string, number> = {
  AP: 10000,
  SA: 100000,
  PPT: 10,
  PT: 20,
  Age: 35,
  TotalPremiumPaid: 50000,
  SurrenderValue: 40000,
};

export default function Calculator() {
  const [products, setProducts] = useState<Product[]>([]);
  const [selectedProduct, setSelectedProduct] = useState('CENTURY_INCOME');
  const [params, setParams] = useState<Record<string, string>>(
    Object.fromEntries(Object.entries(DEFAULT_PARAMS).map(([k, v]) => [k, String(v)]))
  );
  const [result, setResult] = useState<CalculationResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    getProducts().then(r => setProducts(r.data)).catch(() => {});
  }, []);

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const numParams = Object.fromEntries(
        Object.entries(params).map(([k, v]) => [k, parseFloat(v) || 0])
      );
      const resp = await runCalculation(selectedProduct, null, numParams);
      setResult(resp.data);
    } catch (e: any) {
      setError(e.response?.data?.error || e.message || 'Calculation failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h2>Traditional Product Calculation</h2>
      <div style={{ marginBottom: 16 }}>
        <label style={{ fontWeight: 600 }}>Product: </label>
        <select value={selectedProduct} onChange={e => setSelectedProduct(e.target.value)}
          style={{ marginLeft: 8, padding: '4px 8px' }}>
          {products.map(p => <option key={p.code} value={p.code}>{p.name} ({p.code})</option>)}
          {products.length === 0 && <option value="CENTURY_INCOME">Century Income Plan (CENTURY_INCOME)</option>}
        </select>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 16 }}>
        {Object.entries(params).map(([key, val]) => (
          <div key={key}>
            <label style={{ fontWeight: 500, display: 'block', marginBottom: 2 }}>{key}</label>
            <input type="number" value={val}
              onChange={e => setParams(prev => ({ ...prev, [key]: e.target.value }))}
              style={{ width: '100%', padding: '6px 10px', borderRadius: 4, border: '1px solid #ccc' }} />
          </div>
        ))}
      </div>
      <button onClick={handleCalculate} disabled={loading}
        style={{ padding: '10px 24px', background: '#1a237e', color: 'white', border: 'none', borderRadius: 4, cursor: 'pointer', fontWeight: 600, fontSize: 15 }}>
        {loading ? 'Calculating...' : 'Calculate'}
      </button>
      {error && <div style={{ marginTop: 16, color: '#c62828', background: '#ffebee', padding: 12, borderRadius: 4 }}>{error}</div>}
      {result && (
        <div style={{ marginTop: 24 }}>
          <h3>Results for {result.productCode} (v{result.version})</h3>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#e8eaf6' }}>
                <th style={{ padding: 10, textAlign: 'left', border: '1px solid #c5cae9' }}>Formula</th>
                <th style={{ padding: 10, textAlign: 'right', border: '1px solid #c5cae9' }}>Value</th>
              </tr>
            </thead>
            <tbody>
              {Object.entries(result.results).map(([name, value]) => (
                <tr key={name}>
                  <td style={{ padding: 10, border: '1px solid #e0e0e0', fontWeight: 600 }}>{name}</td>
                  <td style={{ padding: 10, border: '1px solid #e0e0e0', textAlign: 'right' }}>
                    {value.toLocaleString('en-IN', { maximumFractionDigits: 2 })}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

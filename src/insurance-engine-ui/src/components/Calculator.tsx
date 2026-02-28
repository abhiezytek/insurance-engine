import { useState, useEffect } from 'react';
import { BarChart3, AlertCircle } from 'lucide-react';
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

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 2 });

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
    setLoading(true); setError(null); setResult(null);
    try {
      const numParams = Object.fromEntries(
        Object.entries(params).map(([k, v]) => [k, parseFloat(v) || 0])
      );
      const resp = await runCalculation(selectedProduct, null, numParams);
      setResult(resp.data);
    } catch (e: any) {
      setError(e.response?.data?.error || e.message || 'Calculation failed');
    } finally { setLoading(false); }
  };

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Traditional Product Calculation
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">Evaluate all product formulas in execution order.</p>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        {/* Input card */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-6 space-y-5">
            <h3 className="text-sm font-semibold text-slate-500 uppercase tracking-wider">Parameters</h3>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Product</label>
              <select
                value={selectedProduct}
                onChange={e => setSelectedProduct(e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                           focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]"
              >
                {products.map(p => <option key={p.code} value={p.code}>{p.name}</option>)}
                {products.length === 0 && <option value="CENTURY_INCOME">Century Income Plan</option>}
              </select>
            </div>

            <div className="space-y-4">
              {Object.entries(params).map(([key, val]) => (
                <div key={key}>
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">{key}</label>
                  <input
                    type="number"
                    value={val}
                    onChange={e => setParams(prev => ({ ...prev, [key]: e.target.value }))}
                    className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm
                               focus:outline-none focus:ring-2 focus:ring-[#007bff] focus:border-[#007bff]"
                  />
                </div>
              ))}
            </div>

            <button
              onClick={handleCalculate}
              disabled={loading}
              className="w-full py-3 bg-[#004282] text-white rounded-xl font-semibold text-sm
                         hover:bg-[#003370] disabled:opacity-50 transition-colors
                         flex items-center justify-center gap-2"
            >
              {loading ? (
                <span className="inline-block w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <BarChart3 size={16} />
              )}
              {loading ? 'Calculating…' : 'Run Calculation'}
            </button>

            {error && (
              <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-xs text-red-700">
                <AlertCircle size={14} className="mt-0.5 flex-shrink-0" />
                {error}
              </div>
            )}
          </div>
        </div>

        {/* Results card */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100">
              <h3 className="text-base font-bold text-[#004282]">
                {result ? `Results — ${result.productCode} v${result.version}` : 'Calculation Results'}
                <span className="block mt-0.5 w-8 h-0.5 rounded-full bg-[#007bff]" />
              </h3>
            </div>
            {!result && !loading && (
              <div className="px-6 py-16 text-center text-slate-400 text-sm">
                Enter parameters and click <strong>Run Calculation</strong> to see results.
              </div>
            )}
            {result && (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      <th className="px-6 py-3 text-left">Formula</th>
                      <th className="px-6 py-3 text-right">Value (₹)</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {Object.entries(result.results).map(([name, value]) => (
                      <tr key={name} className="hover:bg-slate-50 transition-colors">
                        <td className="px-6 py-3 font-semibold text-slate-700">{name}</td>
                        <td className="px-6 py-3 text-right font-mono text-[#004282] font-bold text-base">
                          {INR(value)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

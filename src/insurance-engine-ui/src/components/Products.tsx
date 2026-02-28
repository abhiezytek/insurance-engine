import { useState, useEffect } from 'react';
import { getProducts, getFormulas, getParameters, deleteFormula } from '../api';
import type { Product, ProductFormula, ProductParameter } from '../api';

export default function Products() {
  const [products, setProducts] = useState<Product[]>([]);
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);
  const [formulas, setFormulas] = useState<ProductFormula[]>([]);
  const [parameters, setParameters] = useState<ProductParameter[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getProducts().then(r => { setProducts(r.data); setLoading(false); }).catch(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedVersionId) { setFormulas([]); setParameters([]); return; }
    getFormulas(selectedVersionId).then(r => setFormulas(r.data)).catch(() => {});
    getParameters(selectedVersionId).then(r => setParameters(r.data)).catch(() => {});
  }, [selectedVersionId]);

  const handleDeleteFormula = async (id: number) => {
    await deleteFormula(id);
    setFormulas(prev => prev.filter(f => f.id !== id));
  };

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <h2>Products & Formulas</h2>
      {products.map(product => (
        <div key={product.id} style={{ marginBottom: 24, border: '1px solid #c5cae9', borderRadius: 6, padding: 16 }}>
          <h3 style={{ margin: 0 }}>{product.name} <span style={{ color: '#888', fontSize: 13 }}>({product.code})</span></h3>
          <p style={{ margin: '4px 0', color: '#555' }}>{product.productType} — {product.insurer?.name}</p>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 8 }}>
            {product.versions.map(v => (
              <button key={v.id} onClick={() => setSelectedVersionId(selectedVersionId === v.id ? null : v.id)}
                style={{
                  padding: '4px 12px', borderRadius: 4, border: '1px solid #7986cb', cursor: 'pointer',
                  background: selectedVersionId === v.id ? '#3949ab' : 'white',
                  color: selectedVersionId === v.id ? 'white' : '#3949ab',
                }}>
                v{v.version} {v.isActive ? '✓' : ''}
              </button>
            ))}
          </div>
          {selectedVersionId && product.versions.some(v => v.id === selectedVersionId) && (
            <div style={{ marginTop: 16 }}>
              <h4>Parameters</h4>
              {parameters.length === 0 ? <p>No parameters</p> : (
                <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 12 }}>
                  <thead><tr style={{ background: '#f5f5f5' }}>
                    <th style={th}>Name</th><th style={th}>Type</th><th style={th}>Required</th><th style={th}>Description</th>
                  </tr></thead>
                  <tbody>
                    {parameters.map(p => <tr key={p.id}>
                      <td style={td}>{p.name}</td><td style={td}>{p.dataType}</td>
                      <td style={td}>{p.isRequired ? 'Yes' : 'No'}</td><td style={td}>{p.description}</td>
                    </tr>)}
                  </tbody>
                </table>
              )}
              <h4>Formulas</h4>
              {formulas.length === 0 ? <p>No formulas</p> : (
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead><tr style={{ background: '#f5f5f5' }}>
                    <th style={th}>#</th><th style={th}>Name</th><th style={th}>Expression</th><th style={th}>Description</th><th style={th}>Actions</th>
                  </tr></thead>
                  <tbody>
                    {formulas.map(f => <tr key={f.id}>
                      <td style={td}>{f.executionOrder}</td><td style={td}><strong>{f.name}</strong></td>
                      <td style={{ ...td, fontFamily: 'monospace', color: '#1a237e' }}>{f.expression}</td>
                      <td style={td}>{f.description}</td>
                      <td style={td}><button onClick={() => handleDeleteFormula(f.id)} style={{ color: 'red', border: 'none', background: 'none', cursor: 'pointer' }}>Delete</button></td>
                    </tr>)}
                  </tbody>
                </table>
              )}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

const th: React.CSSProperties = { padding: '8px 10px', textAlign: 'left', border: '1px solid #e0e0e0', background: '#f5f5f5' };
const td: React.CSSProperties = { padding: '8px 10px', border: '1px solid #e0e0e0' };

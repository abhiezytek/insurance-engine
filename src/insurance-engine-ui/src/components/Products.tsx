import { useState, useEffect } from 'react';
import { Package, Trash2, Plus, Save } from 'lucide-react';
import {
  getProducts,
  getFormulas,
  getParameters,
  deleteFormula,
  createProductWithVersion,
  createVersion,
  getOutputTemplates,
  createOutputTemplate,
  updateOutputTemplate,
} from '../api';
import type { Product, ProductFormula, ProductParameter, OutputTemplate } from '../api';

export default function Products() {
  const [products, setProducts] = useState<Product[]>([]);
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);
  const [formulas, setFormulas] = useState<ProductFormula[]>([]);
  const [parameters, setParameters] = useState<ProductParameter[]>([]);
  const [templates, setTemplates] = useState<OutputTemplate[]>([]);
  const [newProduct, setNewProduct] = useState({ insurerId: 1, name: '', code: '', productType: 'Traditional', version: 'v1' });
  const [newVersion, setNewVersion] = useState<{ productId: number | null; version: string; effectiveDate: string }>({
    productId: null,
    version: 'v2',
    effectiveDate: new Date().toISOString().slice(0, 10),
  });
  const [templateDraft, setTemplateDraft] = useState<{ templateName: string; templateJson: string }>({
    templateName: 'Default Output',
    templateJson: '{ "title": "Benefit Illustration" }',
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getProducts().then(r => { setProducts(r.data); setLoading(false); }).catch(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedVersionId) { setFormulas([]); setParameters([]); return; }
    getFormulas(selectedVersionId).then(r => setFormulas(r.data)).catch(() => {});
    getParameters(selectedVersionId).then(r => setParameters(r.data)).catch(() => {});
    getOutputTemplates(selectedVersionId).then(r => setTemplates(r.data)).catch(() => setTemplates([]));
  }, [selectedVersionId]);

  useEffect(() => {
    if (templates.length > 0) {
      setTemplateDraft({
        templateName: templates[0].templateName,
        templateJson: templates[0].templateJson,
      });
    } else {
      setTemplateDraft({ templateName: 'Default Output', templateJson: '{ "title": "Benefit Illustration" }' });
    }
  }, [templates]);

  const handleDeleteFormula = async (id: number) => {
    await deleteFormula(id);
    setFormulas(prev => prev.filter(f => f.id !== id));
  };

  const handleCreateProduct = async () => {
    if (!newProduct.name || !newProduct.code) return;
    await createProductWithVersion(
      { insurerId: newProduct.insurerId, name: newProduct.name, code: newProduct.code, productType: newProduct.productType },
      newProduct.version,
    );
    const refreshed = await getProducts();
    setProducts(refreshed.data);
    setNewProduct({ insurerId: 1, name: '', code: '', productType: 'Traditional', version: 'v1' });
  };

  const handleCreateVersion = async () => {
    if (!newVersion.productId) return;
    await createVersion({
      productId: newVersion.productId,
      version: newVersion.version,
      isActive: true,
      effectiveDate: newVersion.effectiveDate,
    });
    const refreshed = await getProducts();
    setProducts(refreshed.data);
  };

  const handleSaveTemplate = async () => {
    if (!selectedVersionId) return;
    const payload = {
      productVersionId: selectedVersionId,
      templateName: templateDraft.templateName,
      outputFormat: 'PDF',
      templateJson: templateDraft.templateJson,
    };
    if (templates.length === 0) {
      await createOutputTemplate(payload);
    } else {
      await updateOutputTemplate(templates[0].id, payload);
    }
    const refreshed = await getOutputTemplates(selectedVersionId);
    setTemplates(refreshed.data);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <span className="inline-block w-8 h-8 border-2 border-[#007bff]/30 border-t-[#007bff] rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-[#004282]">
          Products &amp; Formulas
          <span className="block mt-1 w-12 h-1 rounded-full bg-[#007bff]" />
        </h2>
        <p className="mt-2 text-slate-500 text-sm">Browse products, versions, parameters, and formula expressions.</p>
      </div>

      <div className="grid md:grid-cols-2 gap-4">
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-4 space-y-3">
          <div className="flex items-center gap-2 text-sm font-semibold text-[#004282]">
            <Plus size={14} /> Create Product + Version
          </div>
          <input className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]" placeholder="Name" value={newProduct.name} onChange={e => setNewProduct({ ...newProduct, name: e.target.value })} />
          <input className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]" placeholder="Code" value={newProduct.code} onChange={e => setNewProduct({ ...newProduct, code: e.target.value })} />
          <input className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]" placeholder="Product Type" value={newProduct.productType} onChange={e => setNewProduct({ ...newProduct, productType: e.target.value })} />
          <input className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]" placeholder="Initial Version (e.g. v1)" value={newProduct.version} onChange={e => setNewProduct({ ...newProduct, version: e.target.value })} />
          <button onClick={handleCreateProduct} className="bg-[#004282] text-white px-4 py-2 rounded-lg text-sm font-semibold flex items-center gap-2">
            <Save size={14} /> Save Product
          </button>
        </div>
        <div className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] p-4 space-y-3">
          <div className="flex items-center gap-2 text-sm font-semibold text-[#004282]">
            <Plus size={14} /> Add Version to Existing Product
          </div>
          <select
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]"
            value={newVersion.productId ?? ''}
            onChange={e => setNewVersion(v => ({ ...v, productId: e.target.value ? Number(e.target.value) : null }))}
          >
            <option value="">Select product</option>
            {products.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <input className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]" placeholder="Version tag" value={newVersion.version} onChange={e => setNewVersion(v => ({ ...v, version: e.target.value }))} />
          <input className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]" type="date" value={newVersion.effectiveDate} onChange={e => setNewVersion(v => ({ ...v, effectiveDate: e.target.value }))} />
          <button onClick={handleCreateVersion} className="bg-[#004282] text-white px-4 py-2 rounded-lg text-sm font-semibold flex items-center gap-2">
            <Save size={14} /> Add Version
          </button>
        </div>
      </div>

      <div className="space-y-6">
        {products.map(product => (
          <div key={product.id} className="bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.08)] overflow-hidden">
            {/* Product header */}
            <div className="px-6 py-4 border-b border-slate-100 flex items-start gap-3">
              <div className="w-9 h-9 bg-blue-100 rounded-lg flex items-center justify-center flex-shrink-0">
                <Package size={18} className="text-[#007bff]" />
              </div>
              <div className="flex-1">
                <h3 className="font-bold text-[#004282]">
                  {product.name}
                  <span className="ml-2 text-xs font-mono bg-slate-100 text-slate-500 px-2 py-0.5 rounded">
                    {product.code}
                  </span>
                </h3>
                <p className="text-xs text-slate-400 mt-0.5">
                  {product.productType} · {product.insurer?.name}
                </p>
              </div>
              {/* Version pills */}
              <div className="flex gap-2 flex-wrap">
                {product.versions.map(v => (
                  <button
                    key={v.id}
                    onClick={() => setSelectedVersionId(selectedVersionId === v.id ? null : v.id)}
                    className={`
                      px-3 py-1 rounded-full text-xs font-semibold transition-all
                      ${selectedVersionId === v.id
                        ? 'bg-[#004282] text-white shadow-sm'
                        : 'bg-white text-[#004282] border border-[#004282] hover:bg-blue-50'
                      }
                    `}
                  >
                    v{v.version} {v.isActive ? '✓' : ''}
                  </button>
                ))}
              </div>
            </div>

            {/* Version detail panel */}
            {selectedVersionId && product.versions.some(v => v.id === selectedVersionId) && (
              <div className="divide-y divide-slate-100">
                {/* Parameters */}
                <div className="px-6 py-5">
                  <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Parameters</h4>
                  {parameters.length === 0 ? (
                    <p className="text-sm text-slate-400">No parameters defined.</p>
                  ) : (
                    <div className="overflow-x-auto rounded-lg border border-slate-200">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                            <th className="px-4 py-2.5 text-left">Name</th>
                            <th className="px-4 py-2.5 text-left">Type</th>
                            <th className="px-4 py-2.5 text-center">Required</th>
                            <th className="px-4 py-2.5 text-left">Description</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                          {parameters.map(p => (
                            <tr key={p.id} className="hover:bg-slate-50 text-slate-700">
                              <td className="px-4 py-2.5 font-semibold">{p.name}</td>
                              <td className="px-4 py-2.5 font-mono text-xs text-slate-400">{p.dataType}</td>
                              <td className="px-4 py-2.5 text-center">
                                {p.isRequired
                                  ? <span className="text-green-600 font-bold">✓</span>
                                  : <span className="text-slate-300">—</span>}
                              </td>
                              <td className="px-4 py-2.5 text-slate-400 text-xs">{p.description}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>

                {/* Formulas */}
                <div className="px-6 py-5">
                  <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Formulas</h4>
                  {formulas.length === 0 ? (
                    <p className="text-sm text-slate-400">No formulas defined.</p>
                  ) : (
                    <div className="overflow-x-auto rounded-lg border border-slate-200">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="bg-blue-50/50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                            <th className="px-4 py-2.5 text-center w-12">#</th>
                            <th className="px-4 py-2.5 text-left">Name</th>
                            <th className="px-4 py-2.5 text-left">Expression</th>
                            <th className="px-4 py-2.5 text-left">Description</th>
                            <th className="px-4 py-2.5 text-center w-16"></th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                          {formulas.map(f => (
                            <tr key={f.id} className="hover:bg-slate-50 text-slate-700">
                              <td className="px-4 py-2.5 text-center text-slate-400 font-mono text-xs">{f.executionOrder}</td>
                              <td className="px-4 py-2.5 font-bold text-[#004282]">{f.name}</td>
                              <td className="px-4 py-2.5 font-mono text-xs text-[#007bff] bg-blue-50/30">{f.expression}</td>
                              <td className="px-4 py-2.5 text-slate-400 text-xs">{f.description}</td>
                              <td className="px-4 py-2.5 text-center">
                                <button
                                  onClick={() => handleDeleteFormula(f.id)}
                                  className="p-1 text-slate-300 hover:text-[#d32f2f] transition-colors rounded"
                                  title="Delete formula"
                                >
                                  <Trash2 size={14} />
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>

                {/* Output templates */}
                <div className="px-6 py-5">
                  <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Output Template (by version)</h4>
                  <div className="space-y-2">
                    <input
                      className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff]"
                      placeholder="Template name"
                      value={templateDraft.templateName}
                      onChange={e => setTemplateDraft({ ...templateDraft, templateName: e.target.value })}
                    />
                    <textarea
                      className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:ring-2 focus:ring-[#007bff] min-h-[120px] font-mono text-xs"
                      value={templateDraft.templateJson}
                      onChange={e => setTemplateDraft({ ...templateDraft, templateJson: e.target.value })}
                    />
                    <div className="flex items-center justify-between text-xs text-slate-500">
                      <span>{templates.length === 0 ? 'No template saved yet' : `Saved templates: ${templates.length}`}</span>
                      <button
                        onClick={handleSaveTemplate}
                        className="bg-[#004282] text-white px-3 py-1.5 rounded-lg text-xs font-semibold"
                      >
                        Save Template
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

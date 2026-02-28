import { useState } from 'react';
import { BarChart3, Upload as UploadIcon, Package, TrendingUp } from 'lucide-react';
import Calculator from './components/Calculator';
import Products from './components/Products';
import Upload from './components/Upload';
import BenefitIllustration from './components/BenefitIllustration';

type Tab = 'bi' | 'calculator' | 'products' | 'upload';

const NAV_ITEMS: { id: Tab; label: string; icon: React.ReactNode }[] = [
  { id: 'bi',         label: 'Benefit Illustration', icon: <TrendingUp size={16} /> },
  { id: 'calculator', label: 'Run Calculation',       icon: <BarChart3 size={16} /> },
  { id: 'products',   label: 'Products & Formulas',  icon: <Package size={16} /> },
  { id: 'upload',     label: 'Bulk Upload',           icon: <UploadIcon size={16} /> },
];

export default function App() {
  const [activeTab, setActiveTab] = useState<Tab>('bi');

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Top header */}
      <header className="bg-[#004282] text-white">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-white rounded-lg flex items-center justify-center">
              <span className="text-[#004282] font-extrabold text-sm">SI</span>
            </div>
            <div>
              <h1 className="text-lg font-bold leading-tight">SUD Life Insurance Engine</h1>
              <p className="text-blue-200 text-xs">Century Income — Benefit Illustration System</p>
            </div>
          </div>
          <span className="hidden sm:inline-flex items-center px-3 py-1 rounded-full bg-white/10 text-blue-100 text-xs font-medium border border-white/20">
            Non-Linked · Non-Participating · Savings
          </span>
        </div>
        {/* Red→Navy gradient accent bar */}
        <div className="h-1" style={{ background: 'linear-gradient(to right, #d32f2f 40%, #004282 100%)' }} />
      </header>

      {/* Navigation */}
      <nav className="bg-white border-b border-slate-200 shadow-sm sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex gap-2 py-3 overflow-x-auto scrollbar-hide">
            {NAV_ITEMS.map(item => (
              <button
                key={item.id}
                onClick={() => setActiveTab(item.id)}
                className={`
                  flex items-center gap-2 px-4 py-2 rounded-full text-sm font-semibold
                  whitespace-nowrap transition-all duration-200 select-none
                  ${activeTab === item.id
                    ? 'bg-[#004282] text-white shadow-md'
                    : 'bg-white text-[#004282] border border-[#004282] hover:bg-blue-50'
                  }
                `}
              >
                {item.icon}
                {item.label}
              </button>
            ))}
          </div>
        </div>
      </nav>

      {/* Page content */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        {activeTab === 'bi'         && <BenefitIllustration />}
        {activeTab === 'calculator' && <Calculator />}
        {activeTab === 'products'   && <Products />}
        {activeTab === 'upload'     && <Upload />}
      </main>

      <footer className="border-t border-slate-200 mt-12 py-4 text-center text-xs text-slate-400">
        © {new Date().getFullYear()} SUD Life Insurance Engine · All calculations are illustrative only
      </footer>
    </div>
  );
}

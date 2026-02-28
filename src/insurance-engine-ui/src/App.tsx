import { useState } from 'react';
import Products from './components/Products';
import Calculator from './components/Calculator';
import Upload from './components/Upload';

type Tab = 'products' | 'calculator' | 'upload';

function App() {
  const [activeTab, setActiveTab] = useState<Tab>('calculator');

  return (
    <div style={{ fontFamily: 'sans-serif', maxWidth: 900, margin: '0 auto', padding: 24 }}>
      <h1 style={{ color: '#1a237e' }}>Insurance Engine</h1>
      <nav style={{ display: 'flex', gap: 12, marginBottom: 24 }}>
        {(['calculator', 'products', 'upload'] as Tab[]).map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            style={{
              padding: '8px 20px', borderRadius: 4, border: 'none', cursor: 'pointer',
              background: activeTab === tab ? '#1a237e' : '#e8eaf6',
              color: activeTab === tab ? 'white' : '#1a237e',
              fontWeight: 600, textTransform: 'capitalize',
            }}
          >
            {tab === 'calculator' ? 'Run Calculation' : tab === 'products' ? 'Products & Formulas' : 'Bulk Upload'}
          </button>
        ))}
      </nav>
      {activeTab === 'calculator' && <Calculator />}
      {activeTab === 'products' && <Products />}
      {activeTab === 'upload' && <Upload />}
    </div>
  );
}

export default App;

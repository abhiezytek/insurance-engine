import { render, screen, fireEvent } from '@testing-library/react';
import { act } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { deriveAnnualisedPremium, SELF_MANAGED_FUNDS } from '../utils/biRules';

vi.mock('../api', () => ({
  getEndowmentConfig: vi.fn().mockResolvedValue({
    data: {
      pptOptions: [7, 10, 12],
      ptOptionsByPpt: { '7': [15, 20], '10': [20, 25], '12': [25] },
      channels: ['Agency'],
      paymentModes: ['Yearly', 'Half-Yearly', 'Quarterly', 'Monthly'],
    },
  }),
  runBenefitIllustration: vi.fn().mockResolvedValue({ data: null }),
  getUlipProducts: vi.fn().mockResolvedValue({ data: [{ code: 'EWEALTH-ROYALE', name: 'SUD Life e-Wealth Royale' }] }),
  runUlipCalculation: vi.fn().mockResolvedValue({
    data: {
      policyNumber: 'TEST',
      customerName: 'Test',
      productName: 'SUD Life e-Wealth Royale',
      option: 'Platinum',
      gender: 'Male',
      entryAge: 30,
      policyTerm: 20,
      ppt: 10,
      annualizedPremium: 60000,
      sumAssured: 600000,
      premiumFrequency: 'Yearly',
      maturityAge: 50,
      premiumInstallment: 0,
      netYield4: 0,
      netYield8: 0,
      maturityBenefit4: 0,
      maturityBenefit8: 0,
      irdaiDisclaimer: '',
      partARows: [],
      partBRows4: [],
      partBRows8: [],
      yearlyTable: [],
    },
  }),
}));

import BenefitIllustration from './BenefitIllustration';
import UlipIllustration from './UlipIllustration';

describe('BI screens', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('hides proposer fields until toggle and derives annualised premium (Endowment)', async () => {
    render(<BenefitIllustration />);

    expect(screen.queryByText(/Proposer Name/i)).not.toBeInTheDocument();

    const toggle = screen.getByLabelText(/Proposer is different/i);
    fireEvent.click(toggle);

    expect(await screen.findByText(/Proposer Name/i)).toBeInTheDocument();

    const premiumInput = screen.getByPlaceholderText('Enter premium');
    fireEvent.change(premiumInput, { target: { value: '10000' } });

    const frequencySelect = screen.getByDisplayValue('Yearly');
    fireEvent.change(frequencySelect, { target: { value: 'Monthly' } });

    const derived = deriveAnnualisedPremium(10000, 'Monthly');
    const annualisedField = screen.getByDisplayValue(String(derived.toFixed(0)));
    expect(annualisedField).toBeInTheDocument();
  });

  it('shows fund dropdown only for self-managed strategy and clears when switched (ULIP)', async () => {
    await act(async () => {
      render(<UlipIllustration />);
    });

    const strategySelect = await screen.findByDisplayValue('Self-Managed Investment Strategy');
    expect(screen.getByText('Fund Allocations')).toBeInTheDocument();
    SELF_MANAGED_FUNDS.forEach(f => expect(screen.getByText(f)).toBeInTheDocument());

    await act(async () => {
      fireEvent.change(strategySelect, { target: { value: 'Age-based Investment Strategy' } });
    });
    expect(screen.queryByText('Fund Allocations')).not.toBeInTheDocument();
  });
});

import { jsPDF } from 'jspdf';
import type { BenefitIllustrationResult, BenefitIllustrationRow } from '../api';

const INR = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 });
const COL_BLUE = '#004282';
const COL_RED = '#d32f2f';
const PAGE_W = 297; // A4 landscape mm
const PAGE_H = 210;
const MARGIN = 14;

// ---------------------------------------------------------------------------
// Shared header
// ---------------------------------------------------------------------------
function addPageHeader(doc: jsPDF, title: string, subtitle: string) {
  // Blue top bar
  doc.setFillColor(COL_BLUE);
  doc.rect(0, 0, PAGE_W, 16, 'F');

  doc.setFontSize(13);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(255, 255, 255);
  doc.text('PrecisionPro', MARGIN, 10);

  doc.setFontSize(9);
  doc.setFont('helvetica', 'normal');
  doc.text('Precision-driven Insurance Calculations', MARGIN, 14.5);

  doc.setTextColor(255, 200, 200);
  //doc.text(new Date().toLocaleDateString('en-IN'), PAGE_W - MARGIN, 10, { align: 'right' });

  // Red accent line
  doc.setFillColor(COL_RED);
  doc.rect(0, 16, PAGE_W, 1.5, 'F');

  // Section title
  doc.setFontSize(11);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(COL_BLUE);
  doc.text(title, MARGIN, 25);

  doc.setFontSize(8.5);
  doc.setFont('helvetica', 'normal');
  doc.setTextColor(80, 80, 80);
  doc.text(subtitle, MARGIN, 31);
}

// ---------------------------------------------------------------------------
// Page footer
// ---------------------------------------------------------------------------
function addFooter(doc: jsPDF) {
  const yFooter = PAGE_H - 8;
  doc.setFillColor(245, 245, 245);
  doc.rect(0, yFooter - 4, PAGE_W, 12, 'F');
  doc.setFontSize(7);
  doc.setTextColor(120, 120, 120);
  doc.text(
    'This illustration is indicative only and is not a contract of insurance. All values subject to applicable taxes.',
    MARGIN,
    yFooter,
  );
  doc.text(
    `Page ${doc.getCurrentPageInfo().pageNumber}`,
    PAGE_W - MARGIN,
    yFooter,
    { align: 'right' },
  );
}

// ---------------------------------------------------------------------------
// Summary info row (label: value, label: value …)
// ---------------------------------------------------------------------------
function addSummaryGrid(
  doc: jsPDF,
  items: [string, string][],
  startY: number,
): number {
  const colW = (PAGE_W - 2 * MARGIN) / 4;
  let x = MARGIN;
  let y = startY;

  doc.setFontSize(8);
  items.forEach(([label, value], idx) => {
    if (idx > 0 && idx % 4 === 0) { y += 10; x = MARGIN; }

    doc.setFont('helvetica', 'bold');
    doc.setTextColor(80, 80, 80);
    doc.text(label, x, y);

    doc.setFont('helvetica', 'normal');
    doc.setTextColor(0, 0, 0);
    doc.text(value, x, y + 5);

    x += colW;
  });
  return y + 12;
}

// ---------------------------------------------------------------------------
// Simple table renderer
// ---------------------------------------------------------------------------
function addTable(
  doc: jsPDF,
  headers: string[],
  rows: string[][],
  startY: number,
): number {
  const colW = (PAGE_W - 2 * MARGIN) / headers.length;
  const rowH = 6.5;
  let y = startY;

  // Header
  doc.setFillColor(COL_BLUE);
  doc.rect(MARGIN, y, PAGE_W - 2 * MARGIN, rowH, 'F');
  doc.setFontSize(7.5);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(255, 255, 255);
  headers.forEach((h, i) => {
    doc.text(h, MARGIN + i * colW + colW / 2, y + 4.5, { align: 'center' });
  });
  y += rowH;

  // Rows
  doc.setFont('helvetica', 'normal');
  rows.forEach((row, ri) => {
    if (ri % 2 === 0) {
      doc.setFillColor(245, 248, 252);
      doc.rect(MARGIN, y, PAGE_W - 2 * MARGIN, rowH, 'F');
    }
    doc.setTextColor(30, 30, 30);
    row.forEach((cell, ci) => {
      doc.text(cell, MARGIN + ci * colW + colW / 2, y + 4.5, { align: 'center' });
    });
    y += rowH;

    // New page if near bottom
    if (y > PAGE_H - 22 && ri < rows.length - 1) {
      addFooter(doc);
      doc.addPage('a4', 'landscape');
      y = 20;
      // Re-draw header
      doc.setFillColor(COL_BLUE);
      doc.rect(MARGIN, y, PAGE_W - 2 * MARGIN, rowH, 'F');
      doc.setFontSize(7.5);
      doc.setFont('helvetica', 'bold');
      doc.setTextColor(255, 255, 255);
      headers.forEach((h, i) => {
        doc.text(h, MARGIN + i * colW + colW / 2, y + 4.5, { align: 'center' });
      });
      y += rowH;
      doc.setFont('helvetica', 'normal');
    }
  });
  return y + 4;
}

// ---------------------------------------------------------------------------
// Endowment BI PDF
// ---------------------------------------------------------------------------
export function downloadEndowmentBiPdf(result: BenefitIllustrationResult) {
  const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });

  addPageHeader(
    doc,
    'Endowment Plan — Benefit Illustration (Annexure Part A)',
    'Pre-issuance illustration. All values in Rs. Figures are illustrative only.',
  );

  const summaryY = addSummaryGrid(doc, [
    ['Annual Premium (AP)', `Rs. ${INR(result.annualPremium)}`],
    ['PPT', `${result.ppt} yrs`],
    ['Policy Term', `${result.policyTerm} yrs`],
    ['Entry Age', `${result.entryAge} yrs`],
    ['Option', result.option],
    ['Channel', result.channel],
    ['Sum Assured on Death', `Rs. ${INR(result.sumAssuredOnDeath)}`],
    ['Guaranteed Maturity Benefit', `Rs. ${INR(result.guaranteedMaturityBenefit)}`],
  ], 38);

  // Section header
  doc.setFontSize(9);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(COL_BLUE);
  doc.text('Part A — Yearly Benefit Table', MARGIN, summaryY);

  addTable(
    doc,
    ['Yr','AP (Rs.)' ,'Total Paid (Rs.)', 'GI (Rs.)', 'LI (Rs.)', 'Total Inc (Rs.)', 'Cumul. SB (Rs.)', 'GSV (Rs.)', 'SSV (Rs.)', 'SV (Rs.)', 'Death Benefit (Rs.)', 'Maturity (Rs.)'],
    (result.yearlyTable as BenefitIllustrationRow[]).map(r => [
      String(r.policyYear),
      INR(r.annualPremium),
      INR(r.totalPremiumsPaid),
      INR(r.guaranteedIncome),
      INR(r.loyaltyIncome),
      INR(r.totalIncome),
      INR(r.cumulativeSurvivalBenefits),
      INR(r.gsv),
      INR(r.ssv),
      INR(r.surrenderValue),
      INR(r.deathBenefit),
      r.policyYear === result.policyTerm ? INR(r.maturityBenefit) : '—',
    ]),
    summaryY + 6,
  );

  addFooter(doc);
  doc.save(`Endowment_BI_AP${result.annualPremium}_PPT${result.ppt}_PT${result.policyTerm}.pdf`);
}

// ---------------------------------------------------------------------------
// YPYG PDF (Annexure Part B)
// Interface compatible with both BenefitIllustrationResult and YpygResult
// ---------------------------------------------------------------------------
export interface YpygPdfResult {
  policyNumber: string;
  annualPremium: number;
  ppt?: number;
  policyTerm: number;
  guaranteedMaturityBenefit?: number;
  maturityValue?: number;
  maxLoanAmount?: number;
  yearlyTable: Array<{
    policyYear: number;
    annualPremium: number;
    totalPremiumsPaid: number;
    guaranteedIncome: number;
    loyaltyIncome: number;
    totalIncome: number;
    surrenderValue: number;
    deathBenefit: number;
    maturityBenefit: number;
  }>;
}

export function downloadYpygPdf(result: YpygPdfResult, policyNumber: string) {
  const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });

  addPageHeader(
    doc,
    'YPYG — You Pay You Get (Annexure Part B)',
    `Policy Number: ${policyNumber || 'N/A'}. All values in Rs.`,
  );

  const summaryY = addSummaryGrid(doc, [
    ['Policy Number', policyNumber || 'N/A'],
    ['Annual Premium', `Rs. ${INR(result.annualPremium)}`],
    ['PPT', result.ppt ? `${result.ppt} yrs` : 'N/A'],
    ['Policy Term', `${result.policyTerm} yrs`],
    ['Maturity Value', `Rs. ${INR(result.guaranteedMaturityBenefit ?? result.maturityValue ?? 0)}`],
    ['Max Loan Amount', `Rs. ${INR(result.maxLoanAmount ?? 0)}`],
  ], 38);

  addTable(
    doc,
    ['Yr', 'AP (Rs.)', 'Total Paid (Rs.)', 'GI (Rs.)', 'LI (Rs.)', 'Total Inc (Rs.)', 'SV (Rs.)', 'Death Benefit (Rs.)', 'Maturity (Rs.)'],
    (result.yearlyTable as BenefitIllustrationRow[]).map(r => [
      String(r.policyYear),
      INR(r.annualPremium),
      INR(r.totalPremiumsPaid),
      INR(r.guaranteedIncome),
      INR(r.loyaltyIncome),
      INR(r.totalIncome),
      INR(r.surrenderValue),
      INR(r.deathBenefit),
      r.policyYear === result.policyTerm ? INR(r.maturityBenefit) : '—',
    ]),
    summaryY + 6,
  );

  addFooter(doc);
  doc.save(`YPYG_${policyNumber || 'policy'}.pdf`);
}

// ---------------------------------------------------------------------------
// ULIP PDF
// ---------------------------------------------------------------------------
export interface UlipPdfRow {
  year: number;
  age: number;
  annualPremium: number;
  premiumInvested: number;
  mortalityCharge: number;
  policyCharge: number;
  fundValue4: number;
  deathBenefit4: number;
  fundValue8: number;
  deathBenefit8: number;
}

export interface UlipPdfData {
  policyNumber: string;
  customerName: string;
  productCode: string;
  gender: string;
  entryAge: number;
  policyTerm: number;
  ppt: number;
  annualizedPremium: number;
  sumAssured: number;
  maturityBenefit4: number;
  maturityBenefit8: number;
  yearlyTable: UlipPdfRow[];
}

export function downloadUlipBiPdf(data: UlipPdfData) {
  const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });

  addPageHeader(
    doc,
    'ULIP — Benefit Illustration (Annexure Part A)',
    'Two scenarios shown: 4% p.a. (conservative) and 8% p.a. (optimistic). All values in Rs. Not guaranteed.',
  );

  const summaryY = addSummaryGrid(doc, [
    ['Customer', data.customerName || data.policyNumber],
    ['Product', data.productCode],
    ['Gender', data.gender],
    ['Entry Age', `${data.entryAge} yrs`],
    ['Annual Premium', `Rs. ${INR(data.annualizedPremium)}`],
    ['PPT', `${data.ppt} yrs`],
    ['Policy Term', `${data.policyTerm} yrs`],
    ['Sum Assured', `Rs. ${INR(data.sumAssured)}`],
    ['Maturity (4%)', `Rs. ${INR(data.maturityBenefit4)}`],
    ['Maturity (8%)', `Rs. ${INR(data.maturityBenefit8)}`],
  ], 38);

  addTable(
    doc,
    ['Yr', 'Age', 'AP (Rs.)', 'Invested (Rs.)', 'MC (Rs.)', 'PC (Rs.)', 'FV @4% (Rs.)', 'DB @4% (Rs.)', 'FV @8% (Rs.)', 'DB @8% (Rs.)'],
    data.yearlyTable.map(r => [
      String(r.year),
      String(r.age),
      INR(r.annualPremium),
      INR(r.premiumInvested),
      INR(r.mortalityCharge),
      INR(r.policyCharge),
      INR(r.fundValue4),
      INR(r.deathBenefit4),
      INR(r.fundValue8),
      INR(r.deathBenefit8),
    ]),
    summaryY + 6,
  );

  addFooter(doc);
  doc.save(`ULIP_${data.policyNumber || 'policy'}_BI.pdf`);
}

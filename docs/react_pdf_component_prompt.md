# React PDF Component Prompt for GitHub Copilot

Generate production-ready React-based PDF components for PrecisionPro YPYG Policy at a Glance documents.

## Scope
Create two PDF variants:
1. ULIP PDF
2. Traditional PDF

Use the following supporting files as design and mapping input:
- 06_pdf_design_prompt.md
- pdf_output_fields.csv
- pdf_sample_payload.json
- pdf_design_tokens.csv

## Requirements

### Architecture
Create:
- `PolicyAtGlancePdf.tsx` as orchestration component
- `UlipPolicyPdf.tsx`
- `TraditionalPolicyPdf.tsx`
- `pdfFormatters.ts`
- `pdfViewModelMapper.ts`
- reusable components such as:
  - `PdfHeader`
  - `LabelValueRow`
  - `SummaryGrid`
  - `ValueCard`
  - `QrCard`
  - `PdfFooter`

### Data handling
Use a view model with consistent keys.
Map raw API payload into PDF-safe values.
Apply fallback rules:
- amount missing -> 0.00
- non-applicable / missing text -> Not Applicable
- date missing -> Not Applicable

### ULIP template behavior
- render ULIP disclaimer
- show fund / maturity wording only in ULIP variant
- preserve Policy at a Glance style

### Traditional template behavior
- render tax / bonus / guaranteed additions wording
- avoid ULIP investment-risk wording
- preserve same overall visual layout with text changes

### Styling
Use design tokens from `pdf_design_tokens.csv`.
Aim for clean single-page A4 layout.
Use card-based structure and two-column summary alignment.

### Output expected
Return complete code for:
1. components
2. types/interfaces
3. formatters/helpers
4. payload-to-view-model mapper
5. example usage with sample payload

Prefer code compatible with either:
- `@react-pdf/renderer`, or
- HTML/CSS React component intended for server-side PDF conversion

If one library must be chosen, prefer `@react-pdf/renderer` and mention any HTML-to-PDF adjustments separately.

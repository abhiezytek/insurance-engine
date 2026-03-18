# PDF Design Prompt for GitHub Copilot

You are designing downloadable one-page Policy at a Glance PDF outputs in PrecisionPro for YPYG.

Create production-ready PDF template code and field mapping logic for two output types:
1. YPYG ULIP PDF
2. YPYG Traditional / Endowment PDF

Do not invent layout sections beyond the sample intent. Follow the attached output formats closely.

## Design objective
Create a clean, branded, one-page PDF with:
- header branding area
- policy summary area
- two-column value comparison area: Till Date vs On Maturity
- key value cards / summary metrics
- call-to-action / QR area
- disclaimer and regulatory footer

The PDF must be readable, print-friendly, and usable for download from web application flow.

## Shared layout structure

### 1. Header
- Company / product branding zone
- Title: Policy at a Glance
- Policy Number prominently displayed

### 2. Policy summary grid
Display fields such as:
- Policy Number
- Customer Name
- Product Details
- Premium Status
- Policy Status
- Product Name
- Risk Cover / Death Benefit
- Premium Payment Term
- Policy Term

### 3. Till Date vs On Maturity section
Display paired fields such as:
- Maturity Date
- Last Premium Due Date
- Next Premium Due Date
- Pending Premium Installments till Maturity
- Premium Frequency

### 4. Value summary section
Display visual value cards / highlighted figures for:
- Value / Maturity Value
- Balance Payable
- Premium Paid till Date
- Revival Amount Due
- Survival Benefit Instalment
- Survival Benefit Start Date
- Unclaimed Fund if Any
- Bonus Accrued till Date
- Survival Benefit Paid till Date

### 5. Motivational / CTA area
Display line similar to:
- Keep paying your premiums to secure your loved ones and enjoy the above benefits

Include QR placeholders/cards for:
- Buy products
- Pay online
- Know health vitals
- Contact us

### 6. Footer
- Regulatory / statutory note
- company contact details
- website
- fraud warning / disclaimer block
- internal document code placeholder if required

## Conditional value display rules
Apply these exact rendering rules:
- If value not available for amount field -> show 0.00
- If value not available for non-applicable field -> show Not Applicable
- Keep date formatting consistent
- Keep currency formatting consistent
- Avoid blank labels in final PDF

## ULIP-specific PDF rules
- Include ULIP risk statement near top or footer:
  - investment risk is borne by policyholder
- Include maturity note based on assumed investment return range if business-approved text exists
- Include market-linked disclaimer and lock-in / fund-risk wording
- Do not use bonus wording as primary benefit language unless a specific field exists

## Traditional-specific PDF rules
- Do not include ULIP market-risk wording
- Include tax / bonus / guaranteed additions note as per output intent
- Include revival quotation note for lapsed policies where applicable
- Use traditional benefit wording instead of fund-value-first language

## Visual design expectations
- Use card-based layout
- Use two-column grid for summary areas
- Use strong headings and muted labels
- Keep enough white space
- Align numeric values to the right where useful
- Keep page within a single printable page if feasible
- Use reusable components for label-value rows

## Implementation requirements
Generate code that supports:
- reusable template component
- separate template variants for ULIP and Traditional
- field mapping layer from API payload to PDF view model
- conditional rendering rules
- formatting helpers for amount/date/text fallback
- logo / QR placeholders if actual assets unavailable

## Output expected from Copilot
Return:
1. suggested component structure
2. PDF view model / DTO
3. field mapping pseudocode
4. template code for ULIP PDF
5. template code for Traditional PDF
6. helper methods for formatting and fallbacks
7. list of configurable labels/disclaimer texts

Prefer implementation that can work with React PDF, HTML-to-PDF, or server-side templating depending existing stack.

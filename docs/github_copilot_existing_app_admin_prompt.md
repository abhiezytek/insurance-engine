# GitHub Copilot change prompt for existing app

You are modifying an already-built application. Do not generate a fresh greenfield project. First inspect the current codebase, identify how authentication, dashboard modules, product flows, Endowment and ULIP implementations, validations, formula logic, Excel processing, and output templates are currently structured, and then propose exact incremental changes file by file. [code_file:72]

## Primary objective

Convert the current static dashboard and hardcoded product setup into an admin-driven, metadata-based architecture with: [code_file:72]
- separate admin login, [code_file:72]
- user-wise permissions in addition to existing roles, [code_file:72]
- dynamic modules for BI, YPYG, and Audit, [code_file:72]
- admin-configured products, [code_file:72]
- admin-configured input screens, validations, formulas, and output templates, [code_file:72]
- Excel master/rule sheet upload and formula-template upload, [code_file:72]
- migration of current Endowment and ULIP into seeded admin-configured products instead of hardcoded flows. [code_file:72]

## Mandatory approach

1. Start by scanning the existing solution and prepare a gap-analysis note. Do not code immediately. [code_file:72]
2. Identify the existing stack, route structure, authentication flow, database access layer, API conventions, UI component conventions, and module/product code paths. [code_file:72]
3. Find the current dashboard module boxes and trace whether they are static arrays, hardcoded cards, or route mappings. [code_file:72]
4. Find all existing Endowment and ULIP code, including calculation logic, validations, input fields, output rendering, BI generation logic, and any Excel dependency. [code_file:72]
5. Reuse maximum existing code. Refactor where needed, do not duplicate business logic unless unavoidable. [code_file:72]
6. Produce changes in phases so the app remains runnable. [code_file:72]

## Required functional changes

### 1. Separate admin login

Implement a separate admin login route and screen. It may share the same underlying auth service if suitable, but the admin entry point must be separate from normal user login and must reject non-admin users. [code_file:72]

Admin auth requirements: [code_file:72]
- separate admin login page/route, [code_file:72]
- admin session handling, [code_file:72]
- route guard for admin-only pages, [code_file:72]
- unauthorized redirect for non-admin users, [code_file:72]
- audit log entry for admin login success/failure if logging framework exists. [code_file:72]

### 2. User-wise permissions

Retain current role-based access if present, but extend it with user-wise module and product permissions. Final access should support: [code_file:72]
- role, [code_file:72]
- module access per user, [code_file:72]
- product access per user where relevant, [code_file:72]
- active/inactive status. [code_file:72]

At minimum admin must be able to: [code_file:72]
- create user login, [code_file:72]
- reset password or trigger first-time password setup, [code_file:72]
- activate/deactivate users, [code_file:72]
- assign accessible modules: BI, YPYG, Audit, [code_file:72]
- optionally assign product-level access under modules. [code_file:72]

### 3. Dynamic dashboard

Replace static dashboard boxes with data-driven cards. Cards should render only for modules accessible to the logged-in user. [code_file:72]

Expected behavior: [code_file:72]
- dashboard cards come from module master and user permission mapping, [code_file:72]
- clicking a module should route into the correct flow, [code_file:72]
- module summary text should come from configuration or master table, not hardcoded constants if feasible, [code_file:72]
- if a user has no access, show no inaccessible modules. [code_file:72]

### 4. Admin master setup

Create admin masters for: [code_file:72]
- Modules, [code_file:72]
- Products, [code_file:72]
- Product versions, [code_file:72]
- Input field definitions, [code_file:72]
- Validation rules, [code_file:72]
- Formula rules, [code_file:72]
- Output templates, [code_file:72]
- Upload batches and uploaded files, [code_file:72]
- User permissions, [code_file:72]
- Audit logs. [code_file:72]

Modules must initially include BI, YPYG, and Audit as seeded values. [code_file:72]

### 5. Product configuration by admin

Admin must be able to add a new product into a selected module. Product setup should support at least: [code_file:72]
- product name, [code_file:72]
- product code, [code_file:72]
- product type, [code_file:72]
- module, [code_file:72]
- version number, [code_file:72]
- effective from date, [code_file:72]
- active flag, [code_file:72]
- description/tag line if applicable, [code_file:72]
- whether Excel-backed calculation is used, [code_file:72]
- whether output template is attached. [code_file:72]

### 6. Seed migration of Endowment and ULIP

Do not keep Endowment and ULIP only as hardcoded product flows. Analyze the existing implementation and move them into the new structure as seeded products. [code_file:72]

Migration expectation: [code_file:72]
- preserve current behavior, [code_file:72]
- seed their product master records, [code_file:72]
- seed their input fields, [code_file:72]
- seed their validations, [code_file:72]
- seed their formula configuration references, [code_file:72]
- seed their output template references. [code_file:72]

Where logic is too complex to fully convert in one go, create an adapter layer so seeded configuration points to existing service implementations first, then progressively refactor. [code_file:72]

### 7. Input screens and validations

Admin must be able to define product input screens and validations. Reuse existing UI renderer if already present; otherwise create a metadata-based renderer. [code_file:72]

Field metadata should support: [code_file:72]
- field key, [code_file:72]
- label, [code_file:72]
- control type, [code_file:72]
- sequence/order, [code_file:72]
- required flag, [code_file:72]
- default value, [code_file:72]
- dropdown source/master source, [code_file:72]
- min/max rules, [code_file:72]
- regex or value-list validation, [code_file:72]
- conditional visibility, [code_file:72]
- conditional mandatory rules, [code_file:72]
- module/product/version association. [code_file:72]

Validation engine should support: [code_file:72]
- inline field validations, [code_file:72]
- cross-field validations, [code_file:72]
- product-specific validation messages, [code_file:72]
- effective versioning. [code_file:72]

### 8. Formula and rules engine

Support both of the following upload patterns because the requirement explicitly includes master/rule sheets versus formula template uploads: [code_file:72]
- master/rule sheet upload, [code_file:72]
- formula template upload. [code_file:72]

Implementation target: [code_file:72]
- formula definitions stored as metadata, [code_file:72]
- product-version aware execution, [code_file:72]
- mapping of formula inputs to field keys, [code_file:72]
- ability to call existing hardcoded calculators through adapters where full conversion is not yet complete, [code_file:72]
- proper validation and error handling for broken formulas or missing references. [code_file:72]

If the existing codebase already parses Excel or uses sheet-driven values, reuse that parser. Extend it so admin-uploaded sheets can populate the relevant master/rule tables. [file:42][file:55][code_file:72]

### 9. Excel upload capability

Admin should be able to upload: [code_file:72]
- master/rule sheets for product configuration, [code_file:72]
- formula template sheets for calculation logic. [code_file:72]

Upload processing should: [code_file:72]
- store file metadata, [code_file:72]
- validate sheet names and expected columns, [code_file:72]
- parse rows into staging tables or structured objects, [code_file:72]
- allow binding uploaded content to selected module/product/version, [code_file:72]
- log import errors, [code_file:72]
- support preview before final publish if feasible within current architecture. [code_file:72]

The sample BI workbooks indicate product logic, charges, BI output tables, and input structures are already sheet-driven in places, so the new admin upload capability should align to that style rather than replacing it blindly. [file:42][file:55][code_file:72]

### 10. Output templates

Admin must be able to configure or attach output templates per product/version. Reuse current output generation where possible. [code_file:72]

Output configuration should support: [code_file:72]
- template name, [code_file:72]
- module/product/version association, [code_file:72]
- template type such as BI/quote/report/PDF view, [code_file:72]
- mapping fields for placeholders, [code_file:72]
- active version. [code_file:72]

### 11. Audit module

The Audit module should not be only another menu card. It should record: [code_file:72]
- admin logins, [code_file:72]
- user creation/update/deactivation, [code_file:72]
- permission changes, [code_file:72]
- product creation and version changes, [code_file:72]
- rule/formula upload events, [code_file:72]
- calculation execution logs if the current application supports trace logging. [code_file:72]

## Technical implementation expectations

### A. Code scanning checklist

Before making changes, locate and document: [code_file:72]
- auth controller/service/middleware, [code_file:72]
- login UI and route definitions, [code_file:72]
- dashboard page and module-card source, [code_file:72]
- existing BI, YPYG, Audit navigation structure, [code_file:72]
- product-specific services for Endowment and ULIP, [code_file:72]
- validation logic location, [code_file:72]
- formula/calculation engine location, [code_file:72]
- Excel import/export utilities, [code_file:72]
- DB models/entities/migrations/schema, [code_file:72]
- existing audit/logging support. [code_file:72]

### B. Database changes

Add tables or equivalent models for: [code_file:72]
- admin_users or user role extension if user table already exists, [code_file:72]
- modules, [code_file:72]
- products, [code_file:72]
- product_versions, [code_file:72]
- user_module_permissions, [code_file:72]
- user_product_permissions, [code_file:72]
- product_fields, [code_file:72]
- validation_rules, [code_file:72]
- formula_definitions, [code_file:72]
- formula_parameters or mappings, [code_file:72]
- output_templates, [code_file:72]
- uploaded_files, [code_file:72]
- upload_batches, [code_file:72]
- audit_logs. [code_file:72]

Do not break current runtime. Add migrations incrementally and seed BI, YPYG, Audit, Endowment, and ULIP data. [code_file:72]

### C. API changes

Add or extend APIs for: [code_file:72]
- admin auth, [code_file:72]
- user CRUD, [code_file:72]
- permission mapping, [code_file:72]
- module master CRUD, [code_file:72]
- product master CRUD, [code_file:72]
- product version CRUD, [code_file:72]
- field/validation/formula/template CRUD, [code_file:72]
- Excel upload and parse, [code_file:72]
- dashboard module retrieval by logged-in user, [code_file:72]
- audit retrieval. [code_file:72]

### D. UI changes

Add or modify UI screens for: [code_file:72]
- separate admin login page, [code_file:72]
- admin dashboard, [code_file:72]
- user management, [code_file:72]
- permission mapping, [code_file:72]
- module management, [code_file:72]
- product management, [code_file:72]
- field and validation designer, [code_file:72]
- formula setup and upload, [code_file:72]
- template setup, [code_file:72]
- audit history. [code_file:72]

### E. Compatibility rule

Where a module or product screen currently expects hardcoded product enums or route names, create compatibility mapping so old references continue to work during transition. [code_file:72]

## Required deliverables from Copilot

Produce the work in this exact order: [code_file:72]

1. **Inventory report**: list existing files/components/services that will be impacted. [code_file:72]
2. **Gap analysis**: explain what is static today and what needs to become metadata-driven. [code_file:72]
3. **DB change plan**: show new tables/columns and seed data strategy. [code_file:72]
4. **Backend patch plan**: exact files to create/update. [code_file:72]
5. **Frontend patch plan**: exact files/components/routes to create/update. [code_file:72]
6. **Migration strategy**: how Endowment and ULIP are seeded without breaking behavior. [code_file:72]
7. **Excel import design**: expected sheet formats for master/rule sheets and formula template sheets. [file:42][file:55][code_file:72]
8. **Code changes**: generate actual code patch suggestions file by file, not generic examples. [code_file:72]
9. **Verification checklist**: tests or manual validation steps. [code_file:72]

## Constraints

- Do not rewrite the whole app. [code_file:72]
- Do not remove current product behavior unless replaced with compatible seeded architecture. [code_file:72]
- Prefer extension and refactor over replacement. [code_file:72]
- Keep names aligned with current code conventions after scanning the project. [code_file:72]
- If the existing codebase uses specific framework patterns, follow those patterns exactly. [code_file:72]
- If any assumption is unclear from the codebase, output a short assumption list before patching. [code_file:72]

## Business interpretation notes

The workbook patterns show that at least some existing products already have structured input sections, rule/master areas, charge tables, formula-driven year/month calculations, and output-oriented BI sheets, which means the new architecture should centralize those concepts rather than hardcoding every new product. [file:42][file:55][code_file:72]

## Final instruction

Act as a codebase refactoring and extension assistant for an existing insurance dashboard application. Inspect first, map the current implementation, then generate exact change guidance and code patches to introduce admin-driven login, permissions, modules, products, formulas, validations, templates, uploads, and seeded migration of Endowment and ULIP with minimum disruption to current code. [code_file:72]

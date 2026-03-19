# Admin-driven metadata transition plan (per `docs/github_copilot_existing_app_admin_prompt.md`)

This document follows the required deliverables order: inventory → gap analysis → DB plan → backend plan → frontend plan → migration strategy → Excel import design → code change suggestions → verification checklist.

## 1) Inventory report (impacted code)
- **Auth**: `Api/Controllers/AuthController.cs`, `Api/Models/AppUser.cs`, `Api/Models/LoginHistory.cs`, `insurance-engine-ui/src/components/LoginPage.tsx`, `insurance-engine-ui/src/context/AuthContext.tsx`, `insurance-engine-ui/src/App.tsx`.
- **Navigation & dashboard**: `insurance-engine-ui/src/components/Dashboard.tsx` (hardcoded `MODULE_CARDS`), `insurance-engine-ui/src/App.tsx` (static `NAV_ITEMS`).
- **Products & calculations**: Endowment/Century in `Api/Controllers/BenefitIllustrationController.cs`, `Api/Services/BenefitCalculationService.cs`; ULIP e-Wealth Royale in `Api/Services/UlipCalculationService.cs`; YPYG in `Api/Controllers/YpygController.cs`; DTOs in `Api/DTOs/*`.
- **Audit**: `Api/Controllers/AuditController.cs`, `Api/Services/AuditService.cs`, UI `insurance-engine-ui/src/components/AuditModule.tsx`.
- **Excel upload**: `Api/Controllers/UploadController.cs`, models `ExcelUploadBatch`/`ExcelUploadRowError`; UI uploader inside Dashboard batch activity.
- **DB & migrations**: `Api/Data/InsuranceDbContext.cs`; migrations in `Api/Migrations/*`; models under `Api/Models/` for products, formulas, conditions, ULIP factors/charges, module access (`ModuleMaster`, `RoleModuleAccess`, `UserMaster`, etc.), logging (`CalculationLog`, `LoginHistory`), PDF/template helpers (`PdfFieldRenderRule`, `ProjectionConfig`).
- **Admin placeholder UI**: `insurance-engine-ui/src/components/AdminMaster.tsx` (currently static stub).

## 2) Gap analysis (static vs. needed metadata)
- **Auth**: Single `/api/auth/login` with role claim only; no dedicated admin login/route guard; AppUser not linked to module/product permissions; LoginHistory exists but no admin/audit linkage.
- **Permissions**: DB has `ModuleMaster`, `RoleMaster`, `RoleModuleAccess`, `UserMaster/UserRole`, `ClientModuleAccess`, but UI does not consume them; modules hardcoded in nav and dashboard; no per-user module/product gating.
- **Dashboard/UI**: `MODULE_CARDS` and `NAV_ITEMS` are static arrays; module descriptions not sourced from DB; no visibility filtering.
- **Products**: Tables for `Product`/`ProductVersion`/`ProductParameter`/`ProductFormula` exist but Endowment/ULIP flows are still hardcoded in controllers/services; validations embedded (e.g., PPT/PT checks, risk rules) instead of metadata-driven.
- **Formulas & rules**: `FormulaMaster`, `ConditionGroup/Condition` available but not wired to BI/ULIP; no adapter layer that executes formulas by product-version.
- **Excel uploads**: `UploadController` supports generic types but lacks module/product/version binding, staging/preview, or seeding into master/rule tables.
- **Templates/output**: PDF helpers exist but no CRUD for output templates tied to products/versions.
- **Audit**: Audit module logs cases/decisions, yet admin operations (user/product/permission changes, uploads) are not logged.
- **Seeding**: No seeded ModuleMaster entries for BI/YPYG/Audit; Endowment/ULIP not seeded into product masters/versions/fields/validations/formulas/templates.

## 3) DB change plan (tables/columns & seed strategy)
- **Users/Admin**: Link `AppUser` ↔ `UserMaster` (UserMasterId FK); add `IsAdmin` flag and `IsActive` to AppUser; store password reset token/expiry. Seed one admin user.
- **Modules & permissions**: Seed `ModuleMaster` (codes: BI, YPYG, AUDIT) and `SubModuleMaster` for BI-Endowment, BI-ULIP, YPYG-Policy/Input, Audit-Payout/Bonus (+dashboards). Add `UserModulePermission` (UserMasterId, ModuleId, SubModuleId?, CanView, CanEdit) and `UserProductPermission` (UserMasterId, ProductId/ProductVersionId, CanCalculate). Option: reuse/extend `RoleModuleAccess` with user override table for minimal schema change.
- **Products & versions**: Reuse `Product`/`ProductVersion`; add fields: `ProductType`, `ModuleId`, `VersionNumber`, `EffectiveFrom`, `EffectiveTo`, `IsActive`, `UsesExcel`, `TemplateCode`. Seed Endowment (Century Income) and ULIP (e-Wealth Royale) rows plus current version entries.
- **Fields & validations**: Introduce `ProductField` (ProductVersionId, FieldKey, Label, ControlType, Sequence, Required, DefaultValue, Min/Max, Regex, OptionsSource, VisibilityRuleJson, MandatoryRuleJson). Add `ValidationRule` (ProductVersionId, FieldKey?, RuleType, Expression/ConditionGroupId, Message, Severity, Order). Leverage existing `ConditionGroup/Condition` for cross-field logic.
- **Formulas**: Add `FormulaDefinition` (ProductVersionId, Name, Expression/TemplatePath, ExecutionOrder, OutputKey, AdapterKey) and `FormulaParameter` (FormulaDefinitionId, FieldKey, SourceType). Map to existing `ProductFormula` if preferred.
- **Templates**: Add `OutputTemplate` (Name, TemplateType, ModuleId, ProductVersionId, Path/BlobRef, PlaceholderMapJson, IsActive).
- **Uploads**: Extend `ExcelUploadBatch` with ModuleId/ProductVersionId/UploadCategory (MasterSheet|RuleSheet|FormulaTemplate), `PreviewJson`, `ErrorReportPath`; tie `ExcelUploadRowError` to batch as-is.
- **Audit**: Extend `AuditLogEntry` to include `ActorUserId`, `EntityType` (User/Product/Permission/Upload/Template), `EntityId`, `Operation`, `MetadataJson`. Seed event types.
- **Seed strategy**: Migration seeds ModuleMaster/SubModules + role “Admin” + admin user; seeds Endowment/ULIP products/versions; seeds key ProductField/ValidationRule/FormulaDefinition/OutputTemplate rows that point to adapters (see migration strategy).

## 4) Backend patch plan (exact files)
- **AuthController**: add `/api/auth/admin-login` that enforces `IsAdmin` + active status; issue JWT with `isAdmin` claim; log login attempts in `LoginHistory` with type.
- **Authorization attributes**: add `[Authorize(Roles = \"Admin\")]` (or policy) to new admin controllers; enforce module/product permission via a custom `PermissionFilter` that reads JWT + DB.
- **Controllers to add/extend**:
  - `AdminUsersController`: CRUD for AppUser/UserMaster, password reset, activation, module/product assignment.
  - `ModulesController`: list modules/submodules per user (`/api/modules/my`), CRUD for modules/submodules (admin-only).
  - `ProductsController`: CRUD for Product/ProductVersion; attach module; toggle active/versioning; fetch field/validation/formula/template definitions.
  - `ProductConfigController`: CRUD for ProductField, ValidationRule, FormulaDefinition/Parameter, OutputTemplate.
  - `UploadsController` extension: bind uploads to module/product/version; parse/preview rows to staging tables; publish to masters.
  - `DashboardController`: returns module cards based on permissions + summary stats (BI/YPYG/Audit counts).
  - `AuditController`: log admin operations by injecting `IAuditLogger`.
- **Services**:
  - `PermissionService`: resolve modules/submodules/products for current user; cached per token.
  - `AdminUserService`: user CRUD, hash via existing PBKDF2 helper.
  - `ProductMetadataService`: read/write product fields, validations, formulas, templates; provides adapter hooks.
  - `ExcelImportService`: validate sheet names/columns, map to ProductField/ValidationRule/FormulaDefinition, write to staging, support preview/publish.
  - `AuditLogger`: writes `AuditLogEntry` for admin/auth/product/permission/upload events.
  - `AdapterRegistry`: maps ProductVersion to calculator delegate (e.g., `BenefitCalculationService`, `UlipCalculationService`) until full formula migration.
- **Seed**: in `SeedData` or new migration `AddAdminMetadataSeeds` add modules/submodules, admin user, Endowment/ULIP product + version + field/validation/formula/template stub rows pointing to adapters; seed permissions for admin to all modules.
- **DTOs**: add request/response models for module/product/permission CRUD, field designer, formula definition, template metadata, upload preview/publish.

## 5) Frontend patch plan (files/components)
- **Routing/auth**: add dedicated Admin login page (`AdminLoginPage.tsx`) with route guard; extend `AuthContext` to store `isAdmin`, `modules`, `products`; refresh permissions on login.
- **Navigation**: replace static `NAV_ITEMS` in `App.tsx` with server-fed modules/submodules; filter by permissions; hide admin-only items for non-admins; add admin dashboard route.
- **Dashboard**: replace `MODULE_CARDS` in `Dashboard.tsx` with API data from `/api/modules/my`; show only accessible modules; descriptions from DB.
- **Admin screens** (new): user management (create/reset/activate), module management, product/version management, field designer, validation rule designer, formula setup (expression + adapter selection), template manager, upload history & preview/publish, audit log viewer. Implement as subroutes under `AdminMaster`.
- **Shared**: API client layer for new admin endpoints; hook upload UI to new upload categories and module/product/version selectors; central permission hook/HOC to guard buttons/routes.
- **Compatibility**: add mapping so existing BI/YPYG/Audit routes still function, using adapter when product metadata references legacy calculators.

## 6) Migration strategy (Endowment & ULIP)
- **Products/versions**: seed Product rows: `BI-ENDOWMENT` (Century Income), `BI-ULIP` (e-Wealth Royale); seed ProductVersion v1 effective today, active.
- **Fields**: create ProductField entries mirroring current UI inputs for both products (age, PPT/PT, premium, channel, fund option, etc.); mark required/defaults as per current DTOs.
- **Validations**: encode existing checks (PPT ≤ PT, age ranges, ppt/pt combos via PptPtRuleBook, ULIP risk preference rules) as ValidationRule + ConditionGroup rows; keep service-level validation until confidence gained.
- **Formulas**: create FormulaDefinition rows that reference adapter keys (`LegacyCenturyCalculator`, `LegacyUlipCalculator`) pointing to existing services; later replace with metadata expressions.
- **Templates**: register OutputTemplate rows pointing to existing PDF export/template codes.
- **Permissions**: seed admin with access to BI, YPYG, Audit modules and both products; optionally seed a demo user with BI-only access to show filtering.
- **Rollout**: keep existing controllers/services but route through metadata: lookup ProductVersion by code, hydrate fields/validations, then call adapter; fallback to legacy flow if metadata missing to avoid regressions.

## 7) Excel import design (master/rule vs. formula templates)
- **Master/Rule sheet upload**: expected columns: `ModuleCode`, `ProductCode`, `Version`, `SheetType (Field|Validation|Rule)`, `FieldKey`, `Label`, `ControlType`, `Sequence`, `Required`, `Default`, `Min`, `Max`, `Regex`, `OptionsSource`, `VisibilityRule`, `MandatoryRule`, `ValidationType`, `Message`, `Severity`, `ConditionExpression` (or `ConditionGroupRef`). Validation: required columns per SheetType; reject unknown Module/Product/Version; preview rows and surface row-level errors.
- **Formula template upload**: columns: `ModuleCode`, `ProductCode`, `Version`, `FormulaName`, `ExecutionOrder`, `OutputKey`, `Expression` (or `TemplatePath`), `AdapterKey`, `Description`, `InputFieldKeys` (comma separated) or parameter rows (`FieldKey`, `SourceType`, `Default`). Validate expression parse; bind to ProductVersion and persist to `FormulaDefinition`/`FormulaParameter`.
- **Template upload (optional)**: `ModuleCode`, `ProductCode`, `Version`, `TemplateName`, `TemplateType`, `FileName`, `PlaceholderMapJson`.
- **Processing**: store file in `ExcelUploadBatch`, parse to staging JSON, provide preview API, publish to master tables in a transaction, log errors to `ExcelUploadRowError`, emit `AuditLogEntry` for upload events.

## 8) Code change suggestions (file-by-file, minimal surface)
- `Api/Controllers/AuthController.cs`: add `AdminLogin` action with role check; include `IsAdmin` claim; log admin login attempts; reject non-admins.
- `Api/Controllers/ModulesController.cs` (new): `GET /api/modules/my` returning modules/submodules filtered by user permissions; `GET/POST/PUT` for module CRUD (admin).
- `Api/Controllers/ProductsController.cs` (new): CRUD for Product/ProductVersion with module linkage and active/effective dates.
- `Api/Controllers/ProductConfigController.cs` (new): CRUD for ProductField, ValidationRule, FormulaDefinition/Parameter, OutputTemplate; preview/publish endpoints for uploads.
- `Api/Controllers/UploadsController.cs`: extend to accept `moduleCode`, `productCode`, `version` and `uploadCategory`; route to `ExcelImportService`.
- `Api/Services/PermissionService.cs` (new): resolve permissions; used by controllers and dashboard endpoint.
- `Api/Services/AdapterRegistry.cs` (new): map ProductVersion → delegate (`BenefitCalculationService` or `UlipCalculationService`).
- `Api/Controllers/DashboardController.cs` (new): serve dashboard cards and stats filtered by permissions.
- `Api/Data/SeedData.cs` or new migration: seed modules/submodules, roles, admin user, Endowment/ULIP products/versions/fields/validations/formulas/templates, and permissions.
- `insurance-engine-ui/src/context/AuthContext.tsx`: store `isAdmin`, `modules`, `products`; refresh on login; provide `hasModule`/`hasProduct` helpers.
- `insurance-engine-ui/src/components/AdminLoginPage.tsx` (new) + route wiring in `App.tsx`.
- `insurance-engine-ui/src/components/Dashboard.tsx`: replace `MODULE_CARDS` with API-driven cards; show only permitted modules; clickable navigation respecting module/submodule route mapping.
- `insurance-engine-ui/src/App.tsx`: build nav from permissions instead of static `NAV_ITEMS`; guard admin pages by `isAdmin`; show/hide BI/YPYG/Audit submenus based on permissions.
- `insurance-engine-ui/src/components/AdminMaster.tsx`: replace stub with tabs for Users, Modules, Products/Versions, Fields, Validations, Formulas, Templates, Uploads, Audit Logs; reuse existing UI patterns (cards/tables/forms).
- `insurance-engine-ui/src/components/shared` (new): small components for permission-guarded buttons, module/product pickers, upload preview tables.

## 9) Verification checklist
- **Automated**: `dotnet test InsuranceEngine.slnx -v minimal`; targeted API tests for new admin endpoints if added; UI unit tests (if added) for permission hook logic.
- **Manual**:
  1. Admin login succeeds; non-admin rejected at admin route; LoginHistory logs event.
  2. Dashboard shows only permitted modules per user; switching user updates cards/nav.
  3. Create user, assign BI-only → BI routes work, YPYG/Audit hidden/blocked.
  4. Create product version, define fields/validations, upload master sheet → fields render; invalid rows surface errors.
  5. Run Endowment/ULIP calculations via metadata-backed adapters; results match legacy outputs.
  6. Upload formula template and bind; calculator executes with new formula; errors handled.
  7. Output template attached and used in PDF/download.
  8. Audit log records admin CRUD, permission changes, uploads, and calculations.

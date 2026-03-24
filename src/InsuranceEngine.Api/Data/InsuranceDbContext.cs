using InsuranceEngine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceEngine.Api.Data;

public class InsuranceDbContext : DbContext
{
    public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options) { }

    public DbSet<Insurer> Insurers => Set<Insurer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVersion> ProductVersions => Set<ProductVersion>();
    public DbSet<ProductParameter> ProductParameters => Set<ProductParameter>();
    public DbSet<ProductFormula> ProductFormulas => Set<ProductFormula>();
    public DbSet<ConditionGroup> ConditionGroups => Set<ConditionGroup>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<ExcelUploadBatch> ExcelUploadBatches => Set<ExcelUploadBatch>();
    public DbSet<ExcelUploadRowError> ExcelUploadRowErrors => Set<ExcelUploadRowError>();
    public DbSet<OutputTemplate> OutputTemplates => Set<OutputTemplate>();
    public DbSet<GmbFactor> GmbFactors => Set<GmbFactor>();
    public DbSet<GsvFactor> GsvFactors => Set<GsvFactor>();
    public DbSet<SsvFactor> SsvFactors => Set<SsvFactor>();
    public DbSet<LoyaltyFactor> LoyaltyFactors => Set<LoyaltyFactor>();
    public DbSet<DeferredIncomeFactor> DeferredIncomeFactors => Set<DeferredIncomeFactor>();
    public DbSet<IncomeScheduleRate> IncomeScheduleRates => Set<IncomeScheduleRate>();
    public DbSet<RevivalInterestRate> RevivalInterestRates => Set<RevivalInterestRate>();
    public DbSet<ArbRate> ArbRates => Set<ArbRate>();
    public DbSet<UlipFundFmc> UlipFundFmcs => Set<UlipFundFmc>();
    public DbSet<PdfFieldRenderRule> PdfFieldRenderRules => Set<PdfFieldRenderRule>();
    public DbSet<ProjectionConfig> ProjectionConfigs => Set<ProjectionConfig>();
    public DbSet<MortalityRate> MortalityRates => Set<MortalityRate>();
    public DbSet<UlipCharge> UlipCharges => Set<UlipCharge>();
    public DbSet<UlipIllustrationResult> UlipIllustrationResults => Set<UlipIllustrationResult>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<CalculationLog> CalculationLogs => Set<CalculationLog>();

    // Audit module
    public DbSet<AuditCase> AuditCases => Set<AuditCase>();
    public DbSet<AuditDecision> AuditDecisions => Set<AuditDecision>();
    public DbSet<AuditBatch> AuditBatches => Set<AuditBatch>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    // Payout verification module
    public DbSet<PayoutCase> PayoutCases => Set<PayoutCase>();
    public DbSet<PayoutBatch> PayoutBatches => Set<PayoutBatch>();
    public DbSet<PayoutFile> PayoutFiles => Set<PayoutFile>();
    public DbSet<PayoutWorkflowHistory> PayoutWorkflowHistories => Set<PayoutWorkflowHistory>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // Module access control
    public DbSet<ModuleMaster> ModuleMasters => Set<ModuleMaster>();
    public DbSet<SubModuleMaster> SubModuleMasters => Set<SubModuleMaster>();
    public DbSet<RoleMaster> RoleMasters => Set<RoleMaster>();
    public DbSet<RoleModuleAccess> RoleModuleAccesses => Set<RoleModuleAccess>();
    public DbSet<UserMaster> UserMasters => Set<UserMaster>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ClientMaster> ClientMasters => Set<ClientMaster>();
    public DbSet<ClientModuleAccess> ClientModuleAccesses => Set<ClientModuleAccess>();

    // Configuration
    public DbSet<FormulaMaster> FormulaMasters => Set<FormulaMaster>();
    public DbSet<IntegrationConfig> IntegrationConfigs => Set<IntegrationConfig>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Insurer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Insurer).WithMany(x => x.Products).HasForeignKey(x => x.InsurerId);
        });

        modelBuilder.Entity<ProductVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Product).WithMany(x => x.Versions).HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<ProductParameter>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ProductVersion).WithMany(x => x.Parameters).HasForeignKey(x => x.ProductVersionId);
        });

        modelBuilder.Entity<ProductFormula>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ProductVersion).WithMany(x => x.Formulas).HasForeignKey(x => x.ProductVersionId);
        });

        modelBuilder.Entity<ConditionGroup>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ProductVersion).WithMany(x => x.ConditionGroups).HasForeignKey(x => x.ProductVersionId);
            e.HasOne(x => x.ParentGroup).WithMany(x => x.ChildGroups).HasForeignKey(x => x.ParentGroupId).IsRequired(false);
        });

        modelBuilder.Entity<Condition>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ConditionGroup).WithMany(x => x.Conditions).HasForeignKey(x => x.ConditionGroupId);
        });

        modelBuilder.Entity<ExcelUploadBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.VersionTag).HasMaxLength(100);
        });
        modelBuilder.Entity<OutputTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TemplateName).HasMaxLength(200).IsRequired();
            e.Property(x => x.OutputFormat).HasMaxLength(50).IsRequired();
            e.HasOne<ProductVersion>().WithMany().HasForeignKey(x => x.ProductVersionId);
        });

        modelBuilder.Entity<ExcelUploadRowError>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ExcelUploadBatch).WithMany(x => x.RowErrors).HasForeignKey(x => x.ExcelUploadBatchId);
        });

        modelBuilder.Entity<GmbFactor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Factor).HasColumnType("decimal(18,4)");
            e.HasIndex(x => new { x.Ppt, x.Pt, x.Option }).HasDatabaseName("IX_GmbFactors_Ppt_Pt_Option");
        });
        modelBuilder.Entity<GsvFactor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FactorPercent).HasColumnType("decimal(18,4)");
            e.HasIndex(x => new { x.Ppt, x.Pt, x.PolicyYear }).HasDatabaseName("IX_GsvFactors_Ppt_Pt_PolicyYear");
        });
        modelBuilder.Entity<SsvFactor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Option).HasMaxLength(50).IsRequired();
            e.Property(x => x.Factor1).HasColumnType("decimal(18,4)");
            e.Property(x => x.Factor2).HasColumnType("decimal(18,4)");
            e.HasIndex(x => new { x.Ppt, x.Pt, x.Option, x.PolicyYear }).HasDatabaseName("IX_SsvFactors_Ppt_Pt_Option_PolicyYear");
        });
        modelBuilder.Entity<LoyaltyFactor>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Ppt).HasDatabaseName("IX_LoyaltyFactors_Ppt");
        });
        modelBuilder.Entity<DeferredIncomeFactor>(e => e.HasKey(x => x.Id));

        modelBuilder.Entity<MortalityRate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Gender).HasMaxLength(10);
        });

        modelBuilder.Entity<UlipCharge>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ChargeType).HasMaxLength(100).IsRequired();
            e.Property(x => x.ChargeFrequency).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<UlipIllustrationResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PolicyNumber).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.PolicyNumber);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<CalculationLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Module).HasMaxLength(100);
            e.Property(x => x.ProductType).HasMaxLength(100);
            e.Property(x => x.PolicyNumber).HasMaxLength(100);
            e.Property(x => x.RequestedBy).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(50);
        });

        // ── Audit module ──
        modelBuilder.Entity<AuditCase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PolicyNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.AuditType).HasMaxLength(50);
            e.Property(x => x.InputMode).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Batch).WithMany(x => x.Cases).HasForeignKey(x => x.BatchId).IsRequired(false);
            e.HasIndex(x => x.Status).HasDatabaseName("IX_AuditCases_Status");
            e.HasIndex(x => x.AuditType).HasDatabaseName("IX_AuditCases_AuditType");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_AuditCases_CreatedAt");
        });

        modelBuilder.Entity<AuditDecision>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Decision).HasMaxLength(20);
            e.Property(x => x.PushStatus).HasMaxLength(20);
            e.HasOne(x => x.AuditCase).WithMany(x => x.Decisions).HasForeignKey(x => x.AuditCaseId);
        });

        modelBuilder.Entity<AuditBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(500);
            e.Property(x => x.AuditType).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<AuditLogEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(100);
            e.Property(x => x.Module).HasMaxLength(50);
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.RecordId).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasIndex(x => new { x.Module, x.Action }).HasDatabaseName("IX_AuditLogEntries_Module_Action");
            e.HasIndex(x => x.DoneBy).HasDatabaseName("IX_AuditLogEntries_DoneBy");
            e.HasIndex(x => x.DoneAt).HasDatabaseName("IX_AuditLogEntries_DoneAt");
            e.HasIndex(x => x.Status).HasDatabaseName("IX_AuditLogEntries_Status");
        });

        // ── Payout verification module ──
        modelBuilder.Entity<PayoutCase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PolicyNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.PayoutType).HasMaxLength(50);
            e.Property(x => x.InputMode).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(30);
            e.Property(x => x.CoreSystemAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PrecisionProAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Variance).HasColumnType("decimal(18,2)");
            e.Property(x => x.VariancePct).HasColumnType("decimal(18,4)");
            e.Property(x => x.SumAssured).HasColumnType("decimal(18,2)");
            e.Property(x => x.AnnualPremium).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Batch).WithMany(x => x.Cases).HasForeignKey(x => x.BatchId).IsRequired(false);
            e.HasIndex(x => x.PolicyNumber).HasDatabaseName("IX_PayoutCases_PolicyNumber");
            e.HasIndex(x => x.Status).HasDatabaseName("IX_PayoutCases_Status");
            e.HasIndex(x => x.PayoutType).HasDatabaseName("IX_PayoutCases_PayoutType");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_PayoutCases_CreatedAt");
            e.HasIndex(x => x.CreatedBy).HasDatabaseName("IX_PayoutCases_CreatedBy");
        });

        modelBuilder.Entity<PayoutBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BatchType).HasMaxLength(30);
            e.Property(x => x.FileName).HasMaxLength(500);
            e.Property(x => x.PayoutType).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(30);
            e.HasIndex(x => x.Status).HasDatabaseName("IX_PayoutBatches_Status");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_PayoutBatches_CreatedAt");
        });

        modelBuilder.Entity<PayoutFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileFormat).HasMaxLength(20);
            e.Property(x => x.FileType).HasMaxLength(20);
            e.Property(x => x.StoragePath).HasMaxLength(1000);
            e.Property(x => x.FileHash).HasMaxLength(128);
            e.HasOne(x => x.Batch).WithMany().HasForeignKey(x => x.BatchId).IsRequired(false);
        });

        modelBuilder.Entity<PayoutWorkflowHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.FromStatus).HasMaxLength(30);
            e.Property(x => x.ToStatus).HasMaxLength(30);
            e.Property(x => x.PushStatus).HasMaxLength(20);
            e.Property(x => x.PushReferenceNumber).HasMaxLength(200);
            e.HasOne(x => x.PayoutCase).WithMany(x => x.WorkflowHistory).HasForeignKey(x => x.PayoutCaseId);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.Property(x => x.RelatedModule).HasMaxLength(50);
            e.Property(x => x.RelatedId).HasMaxLength(50);
            e.HasIndex(x => new { x.UserId, x.IsRead });
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_Notifications_CreatedAt");
        });

        // ── Module access control ──
        modelBuilder.Entity<ModuleMaster>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ModuleName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ModuleCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.ModuleCode).IsUnique();
        });

        modelBuilder.Entity<SubModuleMaster>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SubModuleName).HasMaxLength(100).IsRequired();
            e.Property(x => x.SubModuleCode).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Module).WithMany(x => x.SubModules).HasForeignKey(x => x.ModuleId);
        });

        modelBuilder.Entity<RoleMaster>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<RoleModuleAccess>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Role).WithMany(x => x.ModuleAccess).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId);
            e.HasOne(x => x.SubModule).WithMany().HasForeignKey(x => x.SubModuleId).IsRequired(false);
        });

        modelBuilder.Entity<UserMaster>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
            e.HasIndex(x => x.RoleId).HasDatabaseName("IX_UserRoles_RoleId");
        });

        modelBuilder.Entity<ClientMaster>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ClientName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<ClientModuleAccess>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Client).WithMany(x => x.ModuleAccess).HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId);
        });

        modelBuilder.Entity<FormulaMaster>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Uin).HasMaxLength(100);
            e.Property(x => x.FormulaType).HasMaxLength(100);
            e.HasIndex(x => new { x.ProductName, x.Uin, x.FormulaType, x.IsActive })
                .HasDatabaseName("IX_FormulaMasters_Product_Uin_Type_Active");
        });

        modelBuilder.Entity<IntegrationConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ConfigName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<LoginHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(200);
        });

        modelBuilder.Entity<IncomeScheduleRate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(100);
            e.Property(x => x.Option).HasMaxLength(50);
            e.Property(x => x.BenefitType).HasMaxLength(20);
            e.Property(x => x.RateType).HasMaxLength(50);
        });

        modelBuilder.Entity<RevivalInterestRate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(100);
            e.Property(x => x.RateKey).HasMaxLength(100);
            e.Property(x => x.Compounding).HasMaxLength(50);
            e.Property(x => x.BasisDescription).HasMaxLength(500);
        });

        modelBuilder.Entity<ArbRate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(100);
            e.Property(x => x.Gender).HasMaxLength(20);
            e.Property(x => x.UwClass).HasMaxLength(50);
            e.Property(x => x.SmokerStatus).HasMaxLength(20);
            e.Property(x => x.SourceAnnexure).HasMaxLength(100);
            e.Property(x => x.SourceVersion).HasMaxLength(50);
        });

        modelBuilder.Entity<UlipFundFmc>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(100);
            e.Property(x => x.FundCode).HasMaxLength(50);
            e.Property(x => x.FundName).HasMaxLength(200);
            e.Property(x => x.Sfin).HasMaxLength(100);
        });

        modelBuilder.Entity<PdfFieldRenderRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TemplateCode).HasMaxLength(100);
            e.Property(x => x.FieldKey).HasMaxLength(200);
            e.Property(x => x.ProductType).HasMaxLength(50);
            e.Property(x => x.Section).HasMaxLength(100);
            e.Property(x => x.DataType).HasMaxLength(50);
            e.Property(x => x.EmptyDisplayRule).HasMaxLength(50);
            e.Property(x => x.FormatMask).HasMaxLength(50);
            e.Property(x => x.Label).HasMaxLength(200);
            e.Property(x => x.TemplateSource).HasMaxLength(200);
        });

        modelBuilder.Entity<ProjectionConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TemplateCode).HasMaxLength(100);
            e.Property(x => x.ProjectionMethod).HasMaxLength(100);
            e.Property(x => x.AlternateProjectionRateNote).HasMaxLength(500);
        });
    }
}

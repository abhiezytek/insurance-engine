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
    public DbSet<GmbFactor> GmbFactors => Set<GmbFactor>();
    public DbSet<GsvFactor> GsvFactors => Set<GsvFactor>();
    public DbSet<SsvFactor> SsvFactors => Set<SsvFactor>();
    public DbSet<LoyaltyFactor> LoyaltyFactors => Set<LoyaltyFactor>();
    public DbSet<DeferredIncomeFactor> DeferredIncomeFactors => Set<DeferredIncomeFactor>();

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
        });

        modelBuilder.Entity<ExcelUploadRowError>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ExcelUploadBatch).WithMany(x => x.RowErrors).HasForeignKey(x => x.ExcelUploadBatchId);
        });

        modelBuilder.Entity<GmbFactor>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<GsvFactor>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<SsvFactor>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<LoyaltyFactor>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<DeferredIncomeFactor>(e => e.HasKey(x => x.Id));
    }
}

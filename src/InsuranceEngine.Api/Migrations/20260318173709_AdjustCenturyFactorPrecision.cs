using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdjustCenturyFactorPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Factor2",
                table: "SsvFactors",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Factor1",
                table: "SsvFactors",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FactorPercent",
                table: "GsvFactors",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Factor",
                table: "GmbFactors",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "ArbRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AgeMin = table.Column<int>(type: "int", nullable: true),
                    AgeMax = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UwClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SmokerStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AnnualRatePer1000Sar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceAnnexure = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArbRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncomeScheduleRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Option = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BenefitType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ppt = table.Column<int>(type: "int", nullable: false),
                    Pt = table.Column<int>(type: "int", nullable: false),
                    PolicyYear = table.Column<int>(type: "int", nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeScheduleRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PdfFieldRenderRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Section = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmptyDisplayRule = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FormatMask = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TemplateSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfFieldRenderRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectionConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProjectionMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssumeFuturePremiumsPaid = table.Column<bool>(type: "bit", nullable: false),
                    DefaultProjectionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AlternateProjectionRateNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresBusinessConfirmation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevivalInterestRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnnualRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Compounding = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BasisDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevivalInterestRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UlipFundFmcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FundCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FundName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sfin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FmcRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UlipFundFmcs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArbRates");

            migrationBuilder.DropTable(
                name: "IncomeScheduleRates");

            migrationBuilder.DropTable(
                name: "PdfFieldRenderRules");

            migrationBuilder.DropTable(
                name: "ProjectionConfigs");

            migrationBuilder.DropTable(
                name: "RevivalInterestRates");

            migrationBuilder.DropTable(
                name: "UlipFundFmcs");

            migrationBuilder.AlterColumn<decimal>(
                name: "Factor2",
                table: "SsvFactors",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Factor1",
                table: "SsvFactors",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FactorPercent",
                table: "GsvFactors",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Factor",
                table: "GmbFactors",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditMetadataAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVersionId",
                table: "ExcelUploadBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionTag",
                table: "ExcelUploadBatches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAt",
                table: "AuditCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalculationSource",
                table: "AuditCases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FactorVersion",
                table: "AuditCases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaVersion",
                table: "AuditCases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductVersion",
                table: "AuditCases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutputTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OutputFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutputTemplates_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutputTemplates_ProductVersionId",
                table: "OutputTemplates",
                column: "ProductVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutputTemplates");

            migrationBuilder.DropColumn(
                name: "ProductVersionId",
                table: "ExcelUploadBatches");

            migrationBuilder.DropColumn(
                name: "VersionTag",
                table: "ExcelUploadBatches");

            migrationBuilder.DropColumn(
                name: "CalculatedAt",
                table: "AuditCases");

            migrationBuilder.DropColumn(
                name: "CalculationSource",
                table: "AuditCases");

            migrationBuilder.DropColumn(
                name: "FactorVersion",
                table: "AuditCases");

            migrationBuilder.DropColumn(
                name: "FormulaVersion",
                table: "AuditCases");

            migrationBuilder.DropColumn(
                name: "ProductVersion",
                table: "AuditCases");
        }
    }
}

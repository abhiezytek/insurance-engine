using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutVerificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AuditLogEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "AuditLogEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordId",
                table: "AuditLogEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AuditLogEntries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayoutBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PayoutType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false),
                    MatchCount = table.Column<int>(type: "int", nullable: false),
                    MismatchCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayoutCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayoutType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InputMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    CoreSystemAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecisionProAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VariancePct = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyMaturityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SumAssured = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AnnualPremium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PolicyTerm = table.Column<int>(type: "int", nullable: true),
                    PremiumPayingTerm = table.Column<int>(type: "int", nullable: true),
                    ProductVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FactorVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormulaVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalculationSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutCases_PayoutBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "PayoutBatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayoutFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutFiles_PayoutBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "PayoutBatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayoutWorkflowHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutCaseId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PushStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PushReferenceNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PushErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutWorkflowHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutWorkflowHistories_PayoutCases_PayoutCaseId",
                        column: x => x.PayoutCaseId,
                        principalTable: "PayoutCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutCases_BatchId",
                table: "PayoutCases",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutCases_PolicyNumber",
                table: "PayoutCases",
                column: "PolicyNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutFiles_BatchId",
                table: "PayoutFiles",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutWorkflowHistories_PayoutCaseId",
                table: "PayoutWorkflowHistories",
                column: "PayoutCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutFiles");

            migrationBuilder.DropTable(
                name: "PayoutWorkflowHistories");

            migrationBuilder.DropTable(
                name: "PayoutCases");

            migrationBuilder.DropTable(
                name: "PayoutBatches");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "RecordId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AuditLogEntries");
        }
    }
}

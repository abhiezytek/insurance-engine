using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PayoutCases",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Option",
                table: "GmbFactors",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "FormulaMasters",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DoneBy",
                table: "AuditLogEntries",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutCases_CreatedAt",
                table: "PayoutCases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutCases_CreatedBy",
                table: "PayoutCases",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutCases_PayoutType",
                table: "PayoutCases",
                column: "PayoutType");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutCases_Status",
                table: "PayoutCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutBatches_CreatedAt",
                table: "PayoutBatches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutBatches_Status",
                table: "PayoutBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyFactors_Ppt",
                table: "LoyaltyFactors",
                column: "Ppt");

            migrationBuilder.CreateIndex(
                name: "IX_GmbFactors_Ppt_Pt_Option",
                table: "GmbFactors",
                columns: new[] { "Ppt", "Pt", "Option" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaMasters_Product_Uin_Type_Active",
                table: "FormulaMasters",
                columns: new[] { "ProductName", "Uin", "FormulaType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_DoneAt",
                table: "AuditLogEntries",
                column: "DoneAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_DoneBy",
                table: "AuditLogEntries",
                column: "DoneBy");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_Module_Action",
                table: "AuditLogEntries",
                columns: new[] { "Module", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_Status",
                table: "AuditLogEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCases_AuditType",
                table: "AuditCases",
                column: "AuditType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCases_CreatedAt",
                table: "AuditCases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCases_Status",
                table: "AuditCases",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayoutCases_CreatedAt",
                table: "PayoutCases");

            migrationBuilder.DropIndex(
                name: "IX_PayoutCases_CreatedBy",
                table: "PayoutCases");

            migrationBuilder.DropIndex(
                name: "IX_PayoutCases_PayoutType",
                table: "PayoutCases");

            migrationBuilder.DropIndex(
                name: "IX_PayoutCases_Status",
                table: "PayoutCases");

            migrationBuilder.DropIndex(
                name: "IX_PayoutBatches_CreatedAt",
                table: "PayoutBatches");

            migrationBuilder.DropIndex(
                name: "IX_PayoutBatches_Status",
                table: "PayoutBatches");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyFactors_Ppt",
                table: "LoyaltyFactors");

            migrationBuilder.DropIndex(
                name: "IX_GmbFactors_Ppt_Pt_Option",
                table: "GmbFactors");

            migrationBuilder.DropIndex(
                name: "IX_FormulaMasters_Product_Uin_Type_Active",
                table: "FormulaMasters");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_DoneAt",
                table: "AuditLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_DoneBy",
                table: "AuditLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_Module_Action",
                table: "AuditLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_Status",
                table: "AuditLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditCases_AuditType",
                table: "AuditCases");

            migrationBuilder.DropIndex(
                name: "IX_AuditCases_CreatedAt",
                table: "AuditCases");

            migrationBuilder.DropIndex(
                name: "IX_AuditCases_Status",
                table: "AuditCases");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PayoutCases",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Option",
                table: "GmbFactors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "FormulaMasters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DoneBy",
                table: "AuditLogEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}

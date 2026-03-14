using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPtToGsvAndSsvFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all existing rows so they can be re-seeded with PT-keyed data at startup.
            migrationBuilder.Sql("DELETE FROM [GsvFactors]");
            migrationBuilder.Sql("DELETE FROM [SsvFactors]");

            migrationBuilder.AddColumn<string>(
                name: "Option",
                table: "SsvFactors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Pt",
                table: "SsvFactors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pt",
                table: "GsvFactors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SsvFactors_Ppt_Pt_Option_PolicyYear",
                table: "SsvFactors",
                columns: new[] { "Ppt", "Pt", "Option", "PolicyYear" });

            migrationBuilder.CreateIndex(
                name: "IX_GsvFactors_Ppt_Pt_PolicyYear",
                table: "GsvFactors",
                columns: new[] { "Ppt", "Pt", "PolicyYear" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SsvFactors_Ppt_Pt_Option_PolicyYear",
                table: "SsvFactors");

            migrationBuilder.DropIndex(
                name: "IX_GsvFactors_Ppt_Pt_PolicyYear",
                table: "GsvFactors");

            migrationBuilder.DropColumn(
                name: "Option",
                table: "SsvFactors");

            migrationBuilder.DropColumn(
                name: "Pt",
                table: "SsvFactors");

            migrationBuilder.DropColumn(
                name: "Pt",
                table: "GsvFactors");
        }
    }
}

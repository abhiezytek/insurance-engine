using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutFileHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "PayoutFiles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "PayoutFiles");
        }
    }
}

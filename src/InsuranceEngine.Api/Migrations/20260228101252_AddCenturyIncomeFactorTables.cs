using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCenturyIncomeFactorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeferredIncomeFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ppt = table.Column<int>(type: "int", nullable: false),
                    Pt = table.Column<int>(type: "int", nullable: false),
                    PolicyYear = table.Column<int>(type: "int", nullable: false),
                    RatePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeferredIncomeFactors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GmbFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ppt = table.Column<int>(type: "int", nullable: false),
                    Pt = table.Column<int>(type: "int", nullable: false),
                    EntryAgeMin = table.Column<int>(type: "int", nullable: false),
                    EntryAgeMax = table.Column<int>(type: "int", nullable: false),
                    Option = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmbFactors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GsvFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ppt = table.Column<int>(type: "int", nullable: false),
                    PolicyYear = table.Column<int>(type: "int", nullable: false),
                    FactorPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GsvFactors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ppt = table.Column<int>(type: "int", nullable: false),
                    PolicyYearFrom = table.Column<int>(type: "int", nullable: false),
                    PolicyYearTo = table.Column<int>(type: "int", nullable: true),
                    RatePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyFactors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SsvFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ppt = table.Column<int>(type: "int", nullable: false),
                    PolicyYear = table.Column<int>(type: "int", nullable: false),
                    Factor1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Factor2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SsvFactors", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeferredIncomeFactors");

            migrationBuilder.DropTable(
                name: "GmbFactors");

            migrationBuilder.DropTable(
                name: "GsvFactors");

            migrationBuilder.DropTable(
                name: "LoyaltyFactors");

            migrationBuilder.DropTable(
                name: "SsvFactors");
        }
    }
}

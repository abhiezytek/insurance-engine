using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUlipTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MortalityRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MortalityRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UlipCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ChargeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChargeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChargeFrequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PolicyYear = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UlipCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UlipCharges_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UlipIllustrationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Premium = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PremiumInvested = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MortalityCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PolicyCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FundValue4 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FundValue8 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeathBenefit4 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeathBenefit8 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UlipIllustrationResults", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UlipCharges_ProductId",
                table: "UlipCharges",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UlipIllustrationResults_PolicyNumber",
                table: "UlipIllustrationResults",
                column: "PolicyNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MortalityRates");

            migrationBuilder.DropTable(
                name: "UlipCharges");

            migrationBuilder.DropTable(
                name: "UlipIllustrationResults");
        }
    }
}

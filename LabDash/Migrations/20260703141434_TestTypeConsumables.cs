using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class TestTypeConsumables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consumable",
                columns: table => new
                {
                    ConsumableID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    StockLevel = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumable", x => x.ConsumableID);
                });

            migrationBuilder.CreateTable(
                name: "TestTypeConsumables",
                columns: table => new
                {
                    TestTypeConsumableId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    QuantityRequired = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTypeConsumables", x => x.TestTypeConsumableId);
                    table.ForeignKey(
                        name: "FK_TestTypeConsumables_Consumable_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumable",
                        principalColumn: "ConsumableID");
                    table.ForeignKey(
                        name: "FK_TestTypeConsumables_TestType_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestTypeConsumables_ConsumableId",
                table: "TestTypeConsumables",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypeConsumables_TestTypeId",
                table: "TestTypeConsumables",
                column: "TestTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestTypeConsumables");

            migrationBuilder.DropTable(
                name: "Consumable");
        }
    }
}

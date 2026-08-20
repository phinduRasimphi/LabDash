using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class Consumables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumables_Consumable_ConsumableId",
                table: "TestTypeConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypes_TestCategories_TestCategoryId",
                table: "TestTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Consumable",
                table: "Consumable");

            migrationBuilder.RenameTable(
                name: "Consumable",
                newName: "Consumables");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "TestCategories",
                newName: "TestCategoryId");

            migrationBuilder.AlterColumn<int>(
                name: "TestCategoryId",
                table: "TestTypes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasurement",
                table: "TestTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TestCategories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "Consumables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Consumables",
                table: "Consumables",
                column: "ConsumableID");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumables_Consumables_ConsumableId",
                table: "TestTypeConsumables",
                column: "ConsumableId",
                principalTable: "Consumables",
                principalColumn: "ConsumableID");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypes_TestCategories_TestCategoryId",
                table: "TestTypes",
                column: "TestCategoryId",
                principalTable: "TestCategories",
                principalColumn: "TestCategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumables_Consumables_ConsumableId",
                table: "TestTypeConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypes_TestCategories_TestCategoryId",
                table: "TestTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Consumables",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasurement",
                table: "TestTypes");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "Consumables");

            migrationBuilder.RenameTable(
                name: "Consumables",
                newName: "Consumable");

            migrationBuilder.RenameColumn(
                name: "TestCategoryId",
                table: "TestCategories",
                newName: "Id");

            migrationBuilder.AlterColumn<int>(
                name: "TestCategoryId",
                table: "TestTypes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TestCategories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Consumable",
                table: "Consumable",
                column: "ConsumableID");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumables_Consumable_ConsumableId",
                table: "TestTypeConsumables",
                column: "ConsumableId",
                principalTable: "Consumable",
                principalColumn: "ConsumableID");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypes_TestCategories_TestCategoryId",
                table: "TestTypes",
                column: "TestCategoryId",
                principalTable: "TestCategories",
                principalColumn: "Id");
        }
    }
}

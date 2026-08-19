using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddTestTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianAssignments_TestType_TestTypeId",
                table: "TechnicianAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianTestTypes_TestType_TestTypeId",
                table: "TechnicianTestTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_TestType_TestTypeId",
                table: "TestRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TestType_TestCategories_TestCategoryId",
                table: "TestType");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumables_TestType_TestTypeId",
                table: "TestTypeConsumables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestType",
                table: "TestType");

            migrationBuilder.RenameTable(
                name: "TestType",
                newName: "TestTypes");

            migrationBuilder.RenameIndex(
                name: "IX_TestType_TestCategoryId",
                table: "TestTypes",
                newName: "IX_TestTypes_TestCategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestTypes",
                table: "TestTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianAssignments_TestTypes_TestTypeId",
                table: "TechnicianAssignments",
                column: "TestTypeId",
                principalTable: "TestTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianTestTypes_TestTypes_TestTypeId",
                table: "TechnicianTestTypes",
                column: "TestTypeId",
                principalTable: "TestTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_TestTypes_TestTypeId",
                table: "TestRequestItems",
                column: "TestTypeId",
                principalTable: "TestTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumables_TestTypes_TestTypeId",
                table: "TestTypeConsumables",
                column: "TestTypeId",
                principalTable: "TestTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypes_TestCategories_TestCategoryId",
                table: "TestTypes",
                column: "TestCategoryId",
                principalTable: "TestCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianAssignments_TestTypes_TestTypeId",
                table: "TechnicianAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianTestTypes_TestTypes_TestTypeId",
                table: "TechnicianTestTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_TestTypes_TestTypeId",
                table: "TestRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumables_TestTypes_TestTypeId",
                table: "TestTypeConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypes_TestCategories_TestCategoryId",
                table: "TestTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestTypes",
                table: "TestTypes");

            migrationBuilder.RenameTable(
                name: "TestTypes",
                newName: "TestType");

            migrationBuilder.RenameIndex(
                name: "IX_TestTypes_TestCategoryId",
                table: "TestType",
                newName: "IX_TestType_TestCategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestType",
                table: "TestType",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianAssignments_TestType_TestTypeId",
                table: "TechnicianAssignments",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianTestTypes_TestType_TestTypeId",
                table: "TechnicianTestTypes",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_TestType_TestTypeId",
                table: "TestRequestItems",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestType_TestCategories_TestCategoryId",
                table: "TestType",
                column: "TestCategoryId",
                principalTable: "TestCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumables_TestType_TestTypeId",
                table: "TestTypeConsumables",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");
        }
    }
}

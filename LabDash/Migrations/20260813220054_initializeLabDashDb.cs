using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class initializeLabDashDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TestCategoryId",
                table: "TestType",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TestCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestType_TestCategoryId",
                table: "TestType",
                column: "TestCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestType_TestCategories_TestCategoryId",
                table: "TestType",
                column: "TestCategoryId",
                principalTable: "TestCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestType_TestCategories_TestCategoryId",
                table: "TestType");

            migrationBuilder.DropTable(
                name: "TestCategories");

            migrationBuilder.DropIndex(
                name: "IX_TestType_TestCategoryId",
                table: "TestType");

            migrationBuilder.DropColumn(
                name: "TestCategoryId",
                table: "TestType");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");
        }
    }
}

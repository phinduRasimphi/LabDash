using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class FixAllergyCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Allergies");

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_CategoryId",
                table: "Allergies",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Allergies_Categories_CategoryId",
                table: "Allergies",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allergies_Categories_CategoryId",
                table: "Allergies");

            migrationBuilder.DropIndex(
                name: "IX_Allergies_CategoryId",
                table: "Allergies");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Allergies");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Allergies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CategoryId was already added to the database by the
            // previous partially-applied attempt, so we do NOT add it again.

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Medications");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_CategoryId",
                table: "Medications",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_Categories_CategoryId",
                table: "Medications",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medications_Categories_CategoryId",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_CategoryId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Medications");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Medications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class MakeAssignedTechnicianNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItems");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedTechnicianId",
                table: "TestRequestItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItems",
                column: "AssignedTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItems");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedTechnicianId",
                table: "TestRequestItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItems",
                column: "AssignedTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmployeeNumberOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the existing foreign key because
            // AssignedTechnicianId is being changed to nullable.
            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItems");

            // Make AssignedTechnicianId optional
            migrationBuilder.AlterColumn<string>(
                name: "AssignedTechnicianId",
                table: "TestRequestItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // Make EmployeeNumber optional
            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            // Recreate the foreign key without cascade delete
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

            // Make AssignedTechnicianId required again
            migrationBuilder.AlterColumn<string>(
                name: "AssignedTechnicianId",
                table: "TestRequestItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            // Make EmployeeNumber required again
            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Restore the original cascade foreign key
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

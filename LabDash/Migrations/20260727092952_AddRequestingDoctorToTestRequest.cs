using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestingDoctorToTestRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestingDoctorId",
                table: "TestRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_RequestingDoctorId",
                table: "TestRequests",
                column: "RequestingDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequests_AspNetUsers_RequestingDoctorId",
                table: "TestRequests",
                column: "RequestingDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestRequests_AspNetUsers_RequestingDoctorId",
                table: "TestRequests");

            migrationBuilder.DropIndex(
                name: "IX_TestRequests_RequestingDoctorId",
                table: "TestRequests");

            migrationBuilder.DropColumn(
                name: "RequestingDoctorId",
                table: "TestRequests");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentToTestRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_TestRequestItems_TestRequestItemId",
                table: "TestResults");

            migrationBuilder.AddColumn<DateTime>(
                name: "AppointmentDate",
                table: "TestRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppointmentLocation",
                table: "TestRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppointmentNote",
                table: "TestRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_TestRequestItems_TestRequestItemId",
                table: "TestResults",
                column: "TestRequestItemId",
                principalTable: "TestRequestItems",
                principalColumn: "TestRequestItemId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_TestRequestItems_TestRequestItemId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "AppointmentDate",
                table: "TestRequests");

            migrationBuilder.DropColumn(
                name: "AppointmentLocation",
                table: "TestRequests");

            migrationBuilder.DropColumn(
                name: "AppointmentNote",
                table: "TestRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_TestRequestItems_TestRequestItemId",
                table: "TestResults",
                column: "TestRequestItemId",
                principalTable: "TestRequestItems",
                principalColumn: "TestRequestItemId");
        }
    }
}

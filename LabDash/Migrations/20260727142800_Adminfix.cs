using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class Adminfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_TestRequestItems_TestRequestItemId1",
                table: "TestRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_TestRequestItems_TestRequestItemId1",
                table: "TestRequestItems");

            migrationBuilder.DropColumn(
                name: "TestRequestItemId1",
                table: "TestRequestItems");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDate",
                table: "TestResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNote",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedByTechnicianId",
                table: "TestResults",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TechnicianAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_TechnicianAssignments_AspNetUsers_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicianAssignments_TestType_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_VerifiedByTechnicianId",
                table: "TestResults",
                column: "VerifiedByTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_TechnicianId",
                table: "TechnicianAssignments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_TestTypeId",
                table: "TechnicianAssignments",
                column: "TestTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_AspNetUsers_VerifiedByTechnicianId",
                table: "TestResults",
                column: "VerifiedByTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_AspNetUsers_VerifiedByTechnicianId",
                table: "TestResults");

            migrationBuilder.DropTable(
                name: "TechnicianAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_VerifiedByTechnicianId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "VerificationDate",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "VerificationNote",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "VerifiedByTechnicianId",
                table: "TestResults");

            migrationBuilder.AddColumn<int>(
                name: "TestRequestItemId1",
                table: "TestRequestItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestItems_TestRequestItemId1",
                table: "TestRequestItems",
                column: "TestRequestItemId1");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_TestRequestItems_TestRequestItemId1",
                table: "TestRequestItems",
                column: "TestRequestItemId1",
                principalTable: "TestRequestItems",
                principalColumn: "TestRequestItemId");
        }
    }
}

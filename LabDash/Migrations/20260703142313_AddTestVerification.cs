using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddTestVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestVerifications",
                columns: table => new
                {
                    VerificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestRequestItemId = table.Column<int>(type: "int", nullable: false),
                    VerifiedByTechnicianId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerificationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestVerifications", x => x.VerificationId);
                    table.ForeignKey(
                        name: "FK_TestVerifications_AspNetUsers_VerifiedByTechnicianId",
                        column: x => x.VerifiedByTechnicianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestVerifications_TestRequestItems_TestRequestItemId",
                        column: x => x.TestRequestItemId,
                        principalTable: "TestRequestItems",
                        principalColumn: "TestRequestItemId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestVerifications_TestRequestItemId",
                table: "TestVerifications",
                column: "TestRequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TestVerifications_VerifiedByTechnicianId",
                table: "TestVerifications",
                column: "VerifiedByTechnicianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestVerifications");
        }
    }
}

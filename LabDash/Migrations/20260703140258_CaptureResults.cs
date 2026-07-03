using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class CaptureResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientID);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestRequestItemId = table.Column<int>(type: "int", nullable: false),
                    ResultValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Units = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCaptured = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CapturedByTechnicianId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.ResultId);
                    table.ForeignKey(
                        name: "FK_TestResults_AspNetUsers_CapturedByTechnicianId",
                        column: x => x.CapturedByTechnicianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestResults_TestRequestItems_TestRequestItemId",
                        column: x => x.TestRequestItemId,
                        principalTable: "TestRequestItems",
                        principalColumn: "TestRequestItemId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_PatientId",
                table: "TestRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_CapturedByTechnicianId",
                table: "TestResults",
                column: "CapturedByTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestRequestItemId",
                table: "TestResults",
                column: "TestRequestItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequests_Patients_PatientId",
                table: "TestRequests",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestRequests_Patients_PatientId",
                table: "TestRequests");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestRequests_PatientId",
                table: "TestRequests");
        }
    }
}

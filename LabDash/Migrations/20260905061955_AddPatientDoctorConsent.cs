using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientDoctorConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientDoctorConsents",
                columns: table => new
                {
                    ConsentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GrantedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDoctorConsents", x => x.ConsentID);
                    table.ForeignKey(
                        name: "FK_PatientDoctorConsents_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientDoctorConsents_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsentRequestAccess",
                columns: table => new
                {
                    ConsentAccessID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsentID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRequestAccess", x => x.ConsentAccessID);
                    table.ForeignKey(
                        name: "FK_ConsentRequestAccess_PatientDoctorConsents_ConsentID",
                        column: x => x.ConsentID,
                        principalTable: "PatientDoctorConsents",
                        principalColumn: "ConsentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsentRequestAccess_TestRequests_RequestID",
                        column: x => x.RequestID,
                        principalTable: "TestRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRequestAccess_ConsentID",
                table: "ConsentRequestAccess",
                column: "ConsentID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRequestAccess_RequestID",
                table: "ConsentRequestAccess",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDoctorConsents_DoctorId",
                table: "PatientDoctorConsents",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDoctorConsents_PatientID",
                table: "PatientDoctorConsents",
                column: "PatientID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentRequestAccess");

            migrationBuilder.DropTable(
                name: "PatientDoctorConsents");
        }
    }
}

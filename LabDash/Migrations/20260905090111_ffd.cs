using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class ffd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dosage",
                table: "PatientMedications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "PatientMedications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "PatientMedications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PatientMedications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordedByDoctorId",
                table: "PatientMedications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "PatientMedications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiagnosisDate",
                table: "PatientMedicalConditions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PatientMedicalConditions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordedByDoctorId",
                table: "PatientMedicalConditions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "PatientMedicalConditions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PatientAllergies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordedByDoctorId",
                table: "PatientAllergies",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordedDate",
                table: "PatientAllergies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "PatientAllergies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_RecordedByDoctorId",
                table: "PatientMedications",
                column: "RecordedByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalConditions_RecordedByDoctorId",
                table: "PatientMedicalConditions",
                column: "RecordedByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_RecordedByDoctorId",
                table: "PatientAllergies",
                column: "RecordedByDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAllergies_AspNetUsers_RecordedByDoctorId",
                table: "PatientAllergies",
                column: "RecordedByDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedicalConditions_AspNetUsers_RecordedByDoctorId",
                table: "PatientMedicalConditions",
                column: "RecordedByDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedications_AspNetUsers_RecordedByDoctorId",
                table: "PatientMedications",
                column: "RecordedByDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientAllergies_AspNetUsers_RecordedByDoctorId",
                table: "PatientAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedicalConditions_AspNetUsers_RecordedByDoctorId",
                table: "PatientMedicalConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedications_AspNetUsers_RecordedByDoctorId",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedications_RecordedByDoctorId",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedicalConditions_RecordedByDoctorId",
                table: "PatientMedicalConditions");

            migrationBuilder.DropIndex(
                name: "IX_PatientAllergies_RecordedByDoctorId",
                table: "PatientAllergies");

            migrationBuilder.DropColumn(
                name: "Dosage",
                table: "PatientMedications");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "PatientMedications");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "PatientMedications");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PatientMedications");

            migrationBuilder.DropColumn(
                name: "RecordedByDoctorId",
                table: "PatientMedications");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "PatientMedications");

            migrationBuilder.DropColumn(
                name: "DiagnosisDate",
                table: "PatientMedicalConditions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PatientMedicalConditions");

            migrationBuilder.DropColumn(
                name: "RecordedByDoctorId",
                table: "PatientMedicalConditions");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "PatientMedicalConditions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PatientAllergies");

            migrationBuilder.DropColumn(
                name: "RecordedByDoctorId",
                table: "PatientAllergies");

            migrationBuilder.DropColumn(
                name: "RecordedDate",
                table: "PatientAllergies");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "PatientAllergies");
        }
    }
}

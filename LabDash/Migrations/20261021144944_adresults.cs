using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class adresults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_AspNetUsers_CapturedByTechnicianId",
                table: "TestResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_AspNetUsers_VerifiedByTechnicianId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestRequests_RequestingDoctorId",
                table: "TestRequests");

            migrationBuilder.DropIndex(
                name: "IX_TestRequestItems_RequestId",
                table: "TestRequestItems");

            migrationBuilder.AddColumn<string>(
                name: "PreviousComments",
                table: "TestVerifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreviousIsAbnormal",
                table: "TestVerifications",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousResultValue",
                table: "TestVerifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VerificationNote",
                table: "TestResults",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Units",
                table: "TestResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ResultValue",
                table: "TestResults",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceRange",
                table: "TestResults",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "TestResults",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Urgency",
                table: "TestRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ReleaseNote",
                table: "TestRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClinicalNotes",
                table: "TestRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "TestRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledDate",
                table: "TestRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(@"
IF COL_LENGTH('TestRequests', 'ResultsEmailedAt') IS NULL
    ALTER TABLE [TestRequests] ADD [ResultsEmailedAt] datetime2 NULL;");
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestRequestItems",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Medications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MedicalConditions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConditionName",
                table: "MedicalConditions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.Sql(@"
IF COL_LENGTH('AspNetUsers', 'ActivationTokenExpiresUtc') IS NULL
    ALTER TABLE [AspNetUsers] ADD [ActivationTokenExpiresUtc] datetime2 NULL;");

            migrationBuilder.Sql(@"
IF COL_LENGTH('AspNetUsers', 'ActivationTokenHash') IS NULL
    ALTER TABLE [AspNetUsers] ADD [ActivationTokenHash] nvarchar(max) NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Allergies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "SouthAfricanLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suburb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SouthAfricanLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestReviewHistories",
                columns: table => new
                {
                    TestReviewHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestRequestItemId = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReturnedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestReviewHistories", x => x.TestReviewHistoryId);
                    table.ForeignKey(
                        name: "FK_TestReviewHistories_TestRequestItems_TestRequestItemId",
                        column: x => x.TestRequestItemId,
                        principalTable: "TestRequestItems",
                        principalColumn: "TestRequestItemId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_Status_TestRequestItemId",
                table: "TestResults",
                columns: new[] { "Status", "TestRequestItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_RequestingDoctorId_Status",
                table: "TestRequests",
                columns: new[] { "RequestingDoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestItems_RequestId_Status",
                table: "TestRequestItems",
                columns: new[] { "RequestId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestReviewHistories_TestRequestItemId_ReviewedAt",
                table: "TestReviewHistories",
                columns: new[] { "TestRequestItemId", "ReviewedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_AspNetUsers_CapturedByTechnicianId",
                table: "TestResults",
                column: "CapturedByTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_AspNetUsers_VerifiedByTechnicianId",
                table: "TestResults",
                column: "VerifiedByTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_AspNetUsers_CapturedByTechnicianId",
                table: "TestResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_AspNetUsers_VerifiedByTechnicianId",
                table: "TestResults");

            migrationBuilder.DropTable(
                name: "SouthAfricanLocations");

            migrationBuilder.DropTable(
                name: "TestReviewHistories");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_Status_TestRequestItemId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestRequests_RequestingDoctorId_Status",
                table: "TestRequests");

            migrationBuilder.DropIndex(
                name: "IX_TestRequestItems_RequestId_Status",
                table: "TestRequestItems");

            migrationBuilder.DropColumn(
                name: "PreviousComments",
                table: "TestVerifications");

            migrationBuilder.DropColumn(
                name: "PreviousIsAbnormal",
                table: "TestVerifications");

            migrationBuilder.DropColumn(
                name: "PreviousResultValue",
                table: "TestVerifications");

            migrationBuilder.DropColumn(
                name: "CancelledDate",
                table: "TestRequests");

            migrationBuilder.DropColumn(
                name: "ResultsEmailedAt",
                table: "TestRequests");

            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiresUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActivationTokenHash",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "VerificationNote",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Units",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ResultValue",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceRange",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Urgency",
                table: "TestRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ReleaseNote",
                table: "TestRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClinicalNotes",
                table: "TestRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "TestRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestRequestItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Medications",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MedicalConditions",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConditionName",
                table: "MedicalConditions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Allergies",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_RequestingDoctorId",
                table: "TestRequests",
                column: "RequestingDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestItems_RequestId",
                table: "TestRequestItems",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_AspNetUsers_CapturedByTechnicianId",
                table: "TestResults",
                column: "CapturedByTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_AspNetUsers_VerifiedByTechnicianId",
                table: "TestResults",
                column: "VerifiedByTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

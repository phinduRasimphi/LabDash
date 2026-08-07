using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLabSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_AspNetUsers_UserId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Sample_TestRequests_TestRequestId",
                table: "Sample");

            migrationBuilder.DropForeignKey(
                name: "FK_SampleReceives_TestRequests_RequestId",
                table: "SampleReceives");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianTestTypes_AspNetUsers_TechnicianId",
                table: "TechnicianTestTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianTestTypes_TestType_TestTypeId",
                table: "TechnicianTestTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_TestRequests_RequestId",
                table: "TestRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItems_TestType_TestTypeId",
                table: "TestRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequests_AspNetUsers_RequestingDoctorId",
                table: "TestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequests_Patients_PatientId",
                table: "TestRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumables_Consumable_ConsumableId",
                table: "TestTypeConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumables_TestType_TestTypeId",
                table: "TestTypeConsumables");

            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "MedicalConditions");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "SampleTypeLookups");

            migrationBuilder.DropTable(
                name: "TechnicianAssignments");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestVerifications");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestTypeConsumables",
                table: "TestTypeConsumables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestRequests",
                table: "TestRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestRequestItems",
                table: "TestRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_TestRequestItems_RequestId",
                table: "TestRequestItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TechnicianTestTypes",
                table: "TechnicianTestTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SampleReceives",
                table: "SampleReceives");

            migrationBuilder.DropIndex(
                name: "IX_SampleReceives_RequestId",
                table: "SampleReceives");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Patients",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_UserId",
                table: "Patients");

            migrationBuilder.RenameTable(
                name: "TestTypeConsumables",
                newName: "TestTypeConsumable");

            migrationBuilder.RenameTable(
                name: "TestRequests",
                newName: "TestRequest");

            migrationBuilder.RenameTable(
                name: "TestRequestItems",
                newName: "TestRequestItem");

            migrationBuilder.RenameTable(
                name: "TechnicianTestTypes",
                newName: "TechnicianTestType");

            migrationBuilder.RenameTable(
                name: "SampleReceives",
                newName: "SampleReceive");

            migrationBuilder.RenameTable(
                name: "Patients",
                newName: "Patient");

            migrationBuilder.RenameIndex(
                name: "IX_TestTypeConsumables_TestTypeId",
                table: "TestTypeConsumable",
                newName: "IX_TestTypeConsumable_TestTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_TestTypeConsumables_ConsumableId",
                table: "TestTypeConsumable",
                newName: "IX_TestTypeConsumable_ConsumableId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequests_RequestingDoctorId",
                table: "TestRequest",
                newName: "IX_TestRequest_RequestingDoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequests_PatientId",
                table: "TestRequest",
                newName: "IX_TestRequest_PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequestItems_TestTypeId",
                table: "TestRequestItem",
                newName: "IX_TestRequestItem_TestTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequestItems_AssignedTechnicianId",
                table: "TestRequestItem",
                newName: "IX_TestRequestItem_AssignedTechnicianId");

            migrationBuilder.RenameIndex(
                name: "IX_TechnicianTestTypes_TestTypeId",
                table: "TechnicianTestType",
                newName: "IX_TechnicianTestType_TestTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_TechnicianTestTypes_TechnicianId",
                table: "TechnicianTestType",
                newName: "IX_TechnicianTestType_TechnicianId");

            migrationBuilder.AddColumn<int>(
                name: "TestRequestRequestId",
                table: "TestRequestItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TestRequestRequestId",
                table: "SampleReceive",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Patient",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestTypeConsumable",
                table: "TestTypeConsumable",
                column: "TestTypeConsumableId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestRequest",
                table: "TestRequest",
                column: "RequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestRequestItem",
                table: "TestRequestItem",
                column: "TestRequestItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TechnicianTestType",
                table: "TechnicianTestType",
                column: "TechnicianTestTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SampleReceive",
                table: "SampleReceive",
                column: "SampleReceptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Patient",
                table: "Patient",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestItem_TestRequestRequestId",
                table: "TestRequestItem",
                column: "TestRequestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleReceive_TestRequestRequestId",
                table: "SampleReceive",
                column: "TestRequestRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sample_TestRequest_TestRequestId",
                table: "Sample",
                column: "TestRequestId",
                principalTable: "TestRequest",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SampleReceive_TestRequest_TestRequestRequestId",
                table: "SampleReceive",
                column: "TestRequestRequestId",
                principalTable: "TestRequest",
                principalColumn: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianTestType_AspNetUsers_TechnicianId",
                table: "TechnicianTestType",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianTestType_TestType_TestTypeId",
                table: "TechnicianTestType",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequest_AspNetUsers_RequestingDoctorId",
                table: "TestRequest",
                column: "RequestingDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequest_Patient_PatientId",
                table: "TestRequest",
                column: "PatientId",
                principalTable: "Patient",
                principalColumn: "PatientID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItem_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItem",
                column: "AssignedTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItem_TestRequest_TestRequestRequestId",
                table: "TestRequestItem",
                column: "TestRequestRequestId",
                principalTable: "TestRequest",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItem_TestType_TestTypeId",
                table: "TestRequestItem",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumable_Consumable_ConsumableId",
                table: "TestTypeConsumable",
                column: "ConsumableId",
                principalTable: "Consumable",
                principalColumn: "ConsumableID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumable_TestType_TestTypeId",
                table: "TestTypeConsumable",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sample_TestRequest_TestRequestId",
                table: "Sample");

            migrationBuilder.DropForeignKey(
                name: "FK_SampleReceive_TestRequest_TestRequestRequestId",
                table: "SampleReceive");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianTestType_AspNetUsers_TechnicianId",
                table: "TechnicianTestType");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianTestType_TestType_TestTypeId",
                table: "TechnicianTestType");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequest_AspNetUsers_RequestingDoctorId",
                table: "TestRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequest_Patient_PatientId",
                table: "TestRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItem_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItem_TestRequest_TestRequestRequestId",
                table: "TestRequestItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRequestItem_TestType_TestTypeId",
                table: "TestRequestItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumable_Consumable_ConsumableId",
                table: "TestTypeConsumable");

            migrationBuilder.DropForeignKey(
                name: "FK_TestTypeConsumable_TestType_TestTypeId",
                table: "TestTypeConsumable");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestTypeConsumable",
                table: "TestTypeConsumable");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestRequestItem",
                table: "TestRequestItem");

            migrationBuilder.DropIndex(
                name: "IX_TestRequestItem_TestRequestRequestId",
                table: "TestRequestItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestRequest",
                table: "TestRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TechnicianTestType",
                table: "TechnicianTestType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SampleReceive",
                table: "SampleReceive");

            migrationBuilder.DropIndex(
                name: "IX_SampleReceive_TestRequestRequestId",
                table: "SampleReceive");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "TestRequestRequestId",
                table: "TestRequestItem");

            migrationBuilder.DropColumn(
                name: "TestRequestRequestId",
                table: "SampleReceive");

            migrationBuilder.RenameTable(
                name: "TestTypeConsumable",
                newName: "TestTypeConsumables");

            migrationBuilder.RenameTable(
                name: "TestRequestItem",
                newName: "TestRequestItems");

            migrationBuilder.RenameTable(
                name: "TestRequest",
                newName: "TestRequests");

            migrationBuilder.RenameTable(
                name: "TechnicianTestType",
                newName: "TechnicianTestTypes");

            migrationBuilder.RenameTable(
                name: "SampleReceive",
                newName: "SampleReceives");

            migrationBuilder.RenameTable(
                name: "Patient",
                newName: "Patients");

            migrationBuilder.RenameIndex(
                name: "IX_TestTypeConsumable_TestTypeId",
                table: "TestTypeConsumables",
                newName: "IX_TestTypeConsumables_TestTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_TestTypeConsumable_ConsumableId",
                table: "TestTypeConsumables",
                newName: "IX_TestTypeConsumables_ConsumableId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequestItem_TestTypeId",
                table: "TestRequestItems",
                newName: "IX_TestRequestItems_TestTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequestItem_AssignedTechnicianId",
                table: "TestRequestItems",
                newName: "IX_TestRequestItems_AssignedTechnicianId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequest_RequestingDoctorId",
                table: "TestRequests",
                newName: "IX_TestRequests_RequestingDoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRequest_PatientId",
                table: "TestRequests",
                newName: "IX_TestRequests_PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_TechnicianTestType_TestTypeId",
                table: "TechnicianTestTypes",
                newName: "IX_TechnicianTestTypes_TestTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_TechnicianTestType_TechnicianId",
                table: "TechnicianTestTypes",
                newName: "IX_TechnicianTestTypes_TechnicianId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Patients",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestTypeConsumables",
                table: "TestTypeConsumables",
                column: "TestTypeConsumableId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestRequestItems",
                table: "TestRequestItems",
                column: "TestRequestItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestRequests",
                table: "TestRequests",
                column: "RequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TechnicianTestTypes",
                table: "TechnicianTestTypes",
                column: "TechnicianTestTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SampleReceives",
                table: "SampleReceives",
                column: "SampleReceptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Patients",
                table: "Patients",
                column: "PatientID");

            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    AllergyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllergyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.AllergyId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    MedicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.MedicationId);
                });

            migrationBuilder.CreateTable(
                name: "SampleTypeLookups",
                columns: table => new
                {
                    SampleTypeLookupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleTypeLookups", x => x.SampleTypeLookupId);
                });

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

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CapturedByTechnicianId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TestRequestItemId = table.Column<int>(type: "int", nullable: false),
                    VerifiedByTechnicianId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCaptured = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAbnormal = table.Column<bool>(type: "bit", nullable: false),
                    ReferenceRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Units = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerificationNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                        name: "FK_TestResults_AspNetUsers_VerifiedByTechnicianId",
                        column: x => x.VerifiedByTechnicianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestResults_TestRequestItems_TestRequestItemId",
                        column: x => x.TestRequestItemId,
                        principalTable: "TestRequestItems",
                        principalColumn: "TestRequestItemId");
                });

            migrationBuilder.CreateTable(
                name: "TestVerifications",
                columns: table => new
                {
                    VerificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestRequestItemId = table.Column<int>(type: "int", nullable: false),
                    VerifiedByTechnicianId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.UnitId);
                });

            migrationBuilder.CreateTable(
                name: "MedicalConditions",
                columns: table => new
                {
                    MedicalConditionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ConditionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditions", x => x.MedicalConditionId);
                    table.ForeignKey(
                        name: "FK_MedicalConditions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestRequestItems_RequestId",
                table: "TestRequestItems",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleReceives_RequestId",
                table: "SampleReceives",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_CategoryId",
                table: "MedicalConditions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_TechnicianId",
                table: "TechnicianAssignments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_TestTypeId",
                table: "TechnicianAssignments",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_CapturedByTechnicianId",
                table: "TestResults",
                column: "CapturedByTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestRequestItemId",
                table: "TestResults",
                column: "TestRequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_VerifiedByTechnicianId",
                table: "TestResults",
                column: "VerifiedByTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TestVerifications_TestRequestItemId",
                table: "TestVerifications",
                column: "TestRequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TestVerifications_VerifiedByTechnicianId",
                table: "TestVerifications",
                column: "VerifiedByTechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_AspNetUsers_UserId",
                table: "Patients",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sample_TestRequests_TestRequestId",
                table: "Sample",
                column: "TestRequestId",
                principalTable: "TestRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SampleReceives_TestRequests_RequestId",
                table: "SampleReceives",
                column: "RequestId",
                principalTable: "TestRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianTestTypes_AspNetUsers_TechnicianId",
                table: "TechnicianTestTypes",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianTestTypes_TestType_TestTypeId",
                table: "TechnicianTestTypes",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_AspNetUsers_AssignedTechnicianId",
                table: "TestRequestItems",
                column: "AssignedTechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_TestRequests_RequestId",
                table: "TestRequestItems",
                column: "RequestId",
                principalTable: "TestRequests",
                principalColumn: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequestItems_TestType_TestTypeId",
                table: "TestRequestItems",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequests_AspNetUsers_RequestingDoctorId",
                table: "TestRequests",
                column: "RequestingDoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRequests_Patients_PatientId",
                table: "TestRequests",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "PatientID");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumables_Consumable_ConsumableId",
                table: "TestTypeConsumables",
                column: "ConsumableId",
                principalTable: "Consumable",
                principalColumn: "ConsumableID");

            migrationBuilder.AddForeignKey(
                name: "FK_TestTypeConsumables_TestType_TestTypeId",
                table: "TestTypeConsumables",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");
        }
    }
}

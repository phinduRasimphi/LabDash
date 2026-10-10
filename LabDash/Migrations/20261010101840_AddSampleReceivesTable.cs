using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LabDash.Migrations
{
    public partial class AddSampleReceivesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SampleReceives",
                columns: table => new
                {
                    SampleReceptionId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    RequestId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    TechnicianName = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    SampleBarcode = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    SampleType = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    DateTimeReceived = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false),

                    Status = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Notes = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SampleReceives",
                        x => x.SampleReceptionId);

                    table.ForeignKey(
                        name: "FK_SampleReceives_TestRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TestRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleReceives_RequestId",
                table: "SampleReceives",
                column: "RequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SampleReceives");
        }
    }
}

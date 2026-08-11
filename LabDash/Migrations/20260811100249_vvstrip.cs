using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class vvstrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sample_TestRequests_TestRequestId",
                table: "Sample");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sample",
                table: "Sample");

            migrationBuilder.RenameTable(
                name: "Sample",
                newName: "Samples");

            migrationBuilder.RenameIndex(
                name: "IX_Sample_TestRequestId",
                table: "Samples",
                newName: "IX_Samples_TestRequestId");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SampleTypeLookups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Samples",
                table: "Samples",
                column: "SampleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Samples_TestRequests_TestRequestId",
                table: "Samples",
                column: "TestRequestId",
                principalTable: "TestRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Samples_TestRequests_TestRequestId",
                table: "Samples");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Samples",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SampleTypeLookups");

            migrationBuilder.RenameTable(
                name: "Samples",
                newName: "Sample");

            migrationBuilder.RenameIndex(
                name: "IX_Samples_TestRequestId",
                table: "Sample",
                newName: "IX_Sample_TestRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sample",
                table: "Sample",
                column: "SampleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sample_TestRequests_TestRequestId",
                table: "Sample",
                column: "TestRequestId",
                principalTable: "TestRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

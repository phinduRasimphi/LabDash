using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentItemAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsentItemAccesses",
                columns: table => new
                {
                    ConsentItemAccessID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsentID = table.Column<int>(type: "int", nullable: false),
                    TestRequestItemID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentItemAccesses", x => x.ConsentItemAccessID);
                    table.ForeignKey(
                        name: "FK_ConsentItemAccesses_PatientDoctorConsents_ConsentID",
                        column: x => x.ConsentID,
                        principalTable: "PatientDoctorConsents",
                        principalColumn: "ConsentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsentItemAccesses_TestRequestItems_TestRequestItemID",
                        column: x => x.TestRequestItemID,
                        principalTable: "TestRequestItems",
                        principalColumn: "TestRequestItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentItemAccesses_ConsentID_TestRequestItemID",
                table: "ConsentItemAccesses",
                columns: new[] { "ConsentID", "TestRequestItemID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentItemAccesses_TestRequestItemID",
                table: "ConsentItemAccesses",
                column: "TestRequestItemID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentItemAccesses");
        }
    }
}

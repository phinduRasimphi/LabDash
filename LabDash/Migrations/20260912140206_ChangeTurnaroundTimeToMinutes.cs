using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTurnaroundTimeToMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the database column
            migrationBuilder.RenameColumn(
    name: "TurnaroundTimeHours",
    table: "TestTypes",
    newName: "TurnaroundTimeMinutes");

            migrationBuilder.Sql(
                "EXEC sp_executesql N'UPDATE TestTypes SET TurnaroundTimeMinutes = TurnaroundTimeMinutes * 60';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert minutes back to hours
            migrationBuilder.Sql(
                "UPDATE TestTypes SET TurnaroundTimeMinutes = TurnaroundTimeMinutes / 60");

            // Rename the column back
            migrationBuilder.RenameColumn(
                name: "TurnaroundTimeMinutes",
                table: "TestTypes",
                newName: "TurnaroundTimeHours");
        }
    }
}
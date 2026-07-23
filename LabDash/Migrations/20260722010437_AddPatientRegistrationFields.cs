using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CellphoneNumber",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeAddress",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CellphoneNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "HomeAddress",
                table: "Patients");
        }
    }
}

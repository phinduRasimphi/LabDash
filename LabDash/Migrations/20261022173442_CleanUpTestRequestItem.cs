using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabDash.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpTestRequestItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "ReturnedAt",
            //    table: "TestRequestItems");

            //migrationBuilder.DropColumn(
            //    name: "ReturnedBy",
            //    table: "TestRequestItems");

            //migrationBuilder.DropColumn(
            //    name: "ReviewReason",
            //    table: "TestRequestItems");

            //migrationBuilder.DropColumn(
            //    name: "ReviewedAt",
            //    table: "TestRequestItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ReturnedAt",
            //    table: "TestRequestItems",
            //    type: "datetime2",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "ReturnedBy",
            //    table: "TestRequestItems",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "ReviewReason",
            //    table: "TestRequestItems",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ReviewedAt",
            //    table: "TestRequestItems",
            //    type: "datetime2",
            //    nullable: true);
        }
    }
}

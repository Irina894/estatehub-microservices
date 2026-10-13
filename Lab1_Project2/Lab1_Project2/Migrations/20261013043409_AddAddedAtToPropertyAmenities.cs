using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lab1_Project2.Migrations
{
    /// <inheritdoc />
    public partial class AddAddedAtToPropertyAmenities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AddedAt",
                table: "PropertyAmenities",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedAt",
                table: "PropertyAmenities");
        }
    }
}

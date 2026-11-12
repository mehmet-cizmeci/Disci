using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Disci.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ToothSurfaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Surfaces",
                table: "TreatmentItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Surfaces",
                table: "TreatmentItems");
        }
    }
}

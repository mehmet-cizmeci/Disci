using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Disci.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccessAndProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "Procedures",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShareWithAssistants",
                table: "Dentists",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "ShareWithDentists",
                table: "Dentists",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "DentistsManageOwnSharing",
                table: "ClinicProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsersCanEditOwnProfile",
                table: "ClinicProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DentistAccessGrants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DentistId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Access = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DentistAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DentistAccessGrants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DentistAccessGrants_Dentists_DentistId",
                        column: x => x.DentistId,
                        principalTable: "Dentists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DentistAccessGrants_DentistId_UserId",
                table: "DentistAccessGrants",
                columns: new[] { "DentistId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DentistAccessGrants_UserId",
                table: "DentistAccessGrants",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DentistAccessGrants");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "ShareWithAssistants",
                table: "Dentists");

            migrationBuilder.DropColumn(
                name: "ShareWithDentists",
                table: "Dentists");

            migrationBuilder.DropColumn(
                name: "DentistsManageOwnSharing",
                table: "ClinicProfiles");

            migrationBuilder.DropColumn(
                name: "UsersCanEditOwnProfile",
                table: "ClinicProfiles");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "AspNetUsers");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Disci.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SecondLegacySource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentSessions_LegacyId",
                table: "TreatmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentItems_LegacyId",
                table: "TreatmentItems");

            migrationBuilder.DropIndex(
                name: "IX_Payments_LegacyId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PatientPhotos_LegacyId",
                table: "PatientPhotos");

            migrationBuilder.AddColumn<string>(
                name: "LegacySource",
                table: "TreatmentSessions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacySource",
                table: "TreatmentItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacySource",
                table: "Payments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondProtocol",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacySource",
                table: "PatientPhotos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PatientLegacyNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientLegacyNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientLegacyNumbers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_LegacyId_LegacySource",
                table: "TreatmentSessions",
                columns: new[] { "LegacyId", "LegacySource" },
                unique: true,
                filter: "[LegacyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentItems_LegacyId_LegacySource",
                table: "TreatmentItems",
                columns: new[] { "LegacyId", "LegacySource" },
                unique: true,
                filter: "[LegacyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LegacyId_LegacySource",
                table: "Payments",
                columns: new[] { "LegacyId", "LegacySource" },
                unique: true,
                filter: "[LegacyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SecondProtocol",
                table: "Patients",
                column: "SecondProtocol",
                unique: true,
                filter: "[SecondProtocol] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPhotos_LegacyId_LegacySource",
                table: "PatientPhotos",
                columns: new[] { "LegacyId", "LegacySource" },
                unique: true,
                filter: "[LegacyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLegacyNumbers_PatientId",
                table: "PatientLegacyNumbers",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientLegacyNumbers_Source_Number",
                table: "PatientLegacyNumbers",
                columns: new[] { "Source", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientLegacyNumbers");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentSessions_LegacyId_LegacySource",
                table: "TreatmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentItems_LegacyId_LegacySource",
                table: "TreatmentItems");

            migrationBuilder.DropIndex(
                name: "IX_Payments_LegacyId_LegacySource",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Patients_SecondProtocol",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_PatientPhotos_LegacyId_LegacySource",
                table: "PatientPhotos");

            migrationBuilder.DropColumn(
                name: "LegacySource",
                table: "TreatmentSessions");

            migrationBuilder.DropColumn(
                name: "LegacySource",
                table: "TreatmentItems");

            migrationBuilder.DropColumn(
                name: "LegacySource",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SecondProtocol",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LegacySource",
                table: "PatientPhotos");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_LegacyId",
                table: "TreatmentSessions",
                column: "LegacyId",
                unique: true,
                filter: "[LegacyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentItems_LegacyId",
                table: "TreatmentItems",
                column: "LegacyId",
                unique: true,
                filter: "[LegacyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LegacyId",
                table: "Payments",
                column: "LegacyId",
                unique: true,
                filter: "[LegacyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPhotos_LegacyId",
                table: "PatientPhotos",
                column: "LegacyId",
                unique: true,
                filter: "[LegacyId] IS NOT NULL");
        }
    }
}

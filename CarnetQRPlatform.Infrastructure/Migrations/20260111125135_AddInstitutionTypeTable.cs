using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarnetQRPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionTypeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstitutionType",
                table: "Institutions");

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionTypeId",
                table: "Institutions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstitutionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_InstitutionTypeId",
                table: "Institutions",
                column: "InstitutionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionTypes_Name",
                table: "InstitutionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Institutions_InstitutionTypes_InstitutionTypeId",
                table: "Institutions",
                column: "InstitutionTypeId",
                principalTable: "InstitutionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Institutions_InstitutionTypes_InstitutionTypeId",
                table: "Institutions");

            migrationBuilder.DropTable(
                name: "InstitutionTypes");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_InstitutionTypeId",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "InstitutionTypeId",
                table: "Institutions");

            migrationBuilder.AddColumn<int>(
                name: "InstitutionType",
                table: "Institutions",
                type: "integer",
                nullable: true);
        }
    }
}

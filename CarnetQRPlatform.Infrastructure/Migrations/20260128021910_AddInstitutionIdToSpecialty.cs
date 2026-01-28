using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarnetQRPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionIdToSpecialty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Specialties_Name",
                table: "Specialties");

            // Agregar columna como nullable primero
            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "Specialties",
                type: "uuid",
                nullable: true);

            // Asignar InstitutionId a las especialidades existentes usando la primera institución activa
            // Si no hay instituciones activas, usar la primera institución disponible
            migrationBuilder.Sql(@"
                UPDATE ""Specialties""
                SET ""InstitutionId"" = COALESCE(
                    (SELECT ""Id"" FROM ""Institutions"" WHERE ""IsActive"" = true ORDER BY ""Name"" LIMIT 1),
                    (SELECT ""Id"" FROM ""Institutions"" ORDER BY ""Name"" LIMIT 1)
                )
                WHERE ""InstitutionId"" IS NULL;
            ");

            // Verificar que todas las especialidades tengan InstitutionId asignado
            // Si no hay instituciones, la migración fallará aquí (lo cual es correcto)
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    null_count INTEGER;
                BEGIN
                    SELECT COUNT(*) INTO null_count FROM ""Specialties"" WHERE ""InstitutionId"" IS NULL;
                    IF null_count > 0 THEN
                        RAISE EXCEPTION 'No se pueden asignar especialidades: no hay instituciones en la base de datos. Por favor, cree al menos una institución antes de continuar.';
                    END IF;
                END $$;
            ");

            // Ahora hacer la columna NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "InstitutionId",
                table: "Specialties",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_InstitutionId",
                table: "Specialties",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_InstitutionId_Name",
                table: "Specialties",
                columns: new[] { "InstitutionId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Specialties_Institutions_InstitutionId",
                table: "Specialties",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Specialties_Institutions_InstitutionId",
                table: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_Specialties_InstitutionId",
                table: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_Specialties_InstitutionId_Name",
                table: "Specialties");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Specialties");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_Name",
                table: "Specialties",
                column: "Name",
                unique: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarnetQRPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionConfigurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Primero, agregar una columna temporal para el nuevo tipo
            migrationBuilder.AddColumn<int>(
                name: "InstitutionTypeNew",
                table: "Institutions",
                type: "integer",
                nullable: true);

            // Migrar datos: convertir valores de texto a enum
            // "Clínica" o "Clinica" -> 1, "Hospital" -> 2, otros -> NULL
            migrationBuilder.Sql(@"
                UPDATE ""Institutions""
                SET ""InstitutionTypeNew"" = CASE 
                    WHEN LOWER(""InstitutionType"") LIKE '%clínica%' OR LOWER(""InstitutionType"") LIKE '%clinica%' THEN 1
                    WHEN LOWER(""InstitutionType"") LIKE '%hospital%' THEN 2
                    ELSE NULL
                END
                WHERE ""InstitutionType"" IS NOT NULL;
            ");

            // Eliminar la columna antigua
            migrationBuilder.DropColumn(
                name: "InstitutionType",
                table: "Institutions");

            // Renombrar la nueva columna
            migrationBuilder.RenameColumn(
                name: "InstitutionTypeNew",
                table: "Institutions",
                newName: "InstitutionType");

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Institutions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientDataVisibilityConfig",
                table: "Institutions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PhotoEnabled",
                table: "Institutions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QrPublicDisplayMode",
                table: "Institutions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VisibleFields",
                table: "Institutions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PatientDataVisibilityOverride",
                table: "EntityProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "PatientDataVisibilityConfig",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "PhotoEnabled",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "QrPublicDisplayMode",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "VisibleFields",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "PatientDataVisibilityOverride",
                table: "EntityProfiles");

            // Revertir: cambiar de integer a text
            migrationBuilder.AddColumn<string>(
                name: "InstitutionTypeOld",
                table: "Institutions",
                type: "text",
                nullable: true);

            // Convertir valores de enum a texto
            migrationBuilder.Sql(@"
                UPDATE ""Institutions""
                SET ""InstitutionTypeOld"" = CASE 
                    WHEN ""InstitutionType"" = 1 THEN 'Clínica'
                    WHEN ""InstitutionType"" = 2 THEN 'Hospital'
                    ELSE NULL
                END
                WHERE ""InstitutionType"" IS NOT NULL;
            ");

            // Eliminar columna integer
            migrationBuilder.DropColumn(
                name: "InstitutionType",
                table: "Institutions");

            // Renombrar
            migrationBuilder.RenameColumn(
                name: "InstitutionTypeOld",
                table: "Institutions",
                newName: "InstitutionType");
        }
    }
}

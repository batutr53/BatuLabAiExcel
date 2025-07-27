using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BatuLabAiExcel.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseKeyColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LicenseKey",
                table: "Licenses",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            // Update existing licenses with generated keys
            migrationBuilder.Sql(@"
                UPDATE ""Licenses"" 
                SET ""LicenseKey"" = CONCAT(
                    'OFFICE-AI-',
                    CASE 
                        WHEN ""Type"" = 0 THEN 'TRIAL'
                        WHEN ""Type"" = 1 THEN 'MONTHLY'
                        WHEN ""Type"" = 2 THEN 'YEARLY'
                        WHEN ""Type"" = 3 THEN 'LIFETIME'
                        ELSE 'UNKNOWN'
                    END,
                    '-',
                    EXTRACT(EPOCH FROM ""CreatedAt"")::bigint,
                    '-',
                    UPPER(SUBSTRING(MD5(""Id""::text), 1, 8))
                )
                WHERE ""LicenseKey"" = '' OR ""LicenseKey"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenseKey",
                table: "Licenses");
        }
    }
}

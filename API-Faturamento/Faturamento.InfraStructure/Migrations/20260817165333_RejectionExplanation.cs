using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faturamento.InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class RejectionExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rejection_explanation",
                table: "invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rejection_explanation",
                table: "invoices");
        }
    }
}

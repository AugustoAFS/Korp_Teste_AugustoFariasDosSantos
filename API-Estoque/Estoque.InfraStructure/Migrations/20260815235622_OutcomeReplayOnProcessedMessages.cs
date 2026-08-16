using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoque.InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class OutcomeReplayOnProcessedMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "outcome_payload",
                table: "processed_messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "outcome_type",
                table: "processed_messages",
                type: "varchar(100)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "outcome_payload",
                table: "processed_messages");

            migrationBuilder.DropColumn(
                name: "outcome_type",
                table: "processed_messages");
        }
    }
}

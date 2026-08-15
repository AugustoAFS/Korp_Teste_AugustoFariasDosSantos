using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoque.InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "varchar(100)", nullable: false),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    last_error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "varchar(100)", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_messages", x => x.message_id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", nullable: false),
                    balance = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_balance", "[balance] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<byte>(type: "tinyint", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    balance_before = table.Column<int>(type: "int", nullable: false),
                    balance_after = table.Column<int>(type: "int", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    moved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "fk_stock_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_created_at",
                table: "outbox_messages",
                column: "created_at")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages",
                columns: new[] { "published_at", "attempts", "created_at" },
                filter: "[published_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_products_code",
                table: "products",
                column: "code",
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_occurred_at",
                table: "stock_movements",
                column: "occurred_at")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_product",
                table: "stock_movements",
                columns: new[] { "product_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_idempotency",
                table: "stock_movements",
                columns: new[] { "idempotency_key", "product_id" },
                unique: true,
                filter: "[idempotency_key] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "processed_messages");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}

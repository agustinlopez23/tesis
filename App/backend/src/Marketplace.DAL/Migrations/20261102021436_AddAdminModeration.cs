using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_login_count",
                table: "user_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_suspended",
                table: "catalog_products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_suspended",
                table: "businesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "business_categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "admin_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_label = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_actions", x => x.id);
                    table.CheckConstraint("CK_admin_actions_target", "target_type IN ('account', 'business', 'product', 'category')");
                    table.ForeignKey(
                        name: "FK_admin_actions_user_accounts_actor_id",
                        column: x => x.actor_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "business_categories",
                keyColumn: "id",
                keyValue: (short)1,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "business_categories",
                keyColumn: "id",
                keyValue: (short)2,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "business_categories",
                keyColumn: "id",
                keyValue: (short)3,
                column: "is_active",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_categories_name",
                table: "business_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_actions_actor_id",
                table: "admin_actions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_actions_created_at",
                table: "admin_actions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_admin_actions_target_type_target_id",
                table: "admin_actions",
                columns: new[] { "target_type", "target_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_actions");

            migrationBuilder.DropIndex(
                name: "IX_business_categories_name",
                table: "business_categories");

            migrationBuilder.DropColumn(
                name: "failed_login_count",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "is_suspended",
                table: "catalog_products");

            migrationBuilder.DropColumn(
                name: "is_suspended",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "business_categories");
        }
    }
}

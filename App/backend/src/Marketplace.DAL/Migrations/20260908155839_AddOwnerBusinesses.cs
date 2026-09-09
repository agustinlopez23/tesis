using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerBusinesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_categories",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "businesses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    whatsapp = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    location = table.Column<Point>(type: "geography (point,4326)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_businesses", x => x.id);
                    table.CheckConstraint("CK_businesses_status", "status IN ('draft', 'published')");
                    table.ForeignKey(
                        name: "FK_businesses_business_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "business_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_businesses_user_accounts_owner_id",
                        column: x => x.owner_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "business_categories",
                columns: new[] { "id", "name", "slug" },
                values: new object[,]
                {
                    { (short)1, "Almacenes", "almacenes" },
                    { (short)2, "Gastronomía", "gastronomia" },
                    { (short)3, "Indumentaria", "indumentaria" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_categories_slug",
                table: "business_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_businesses_category_id",
                table: "businesses",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_businesses_owner_id",
                table: "businesses",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "businesses");

            migrationBuilder.DropTable(
                name: "business_categories");
        }
    }
}

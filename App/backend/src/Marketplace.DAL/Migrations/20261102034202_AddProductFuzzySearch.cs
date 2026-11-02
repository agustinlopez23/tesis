using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <summary>Typo-tolerant product search: trigram similarity plus bounded edit distance.</summary>
    public partial class AddProductFuzzySearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS fuzzystrmatch;");
            // Same folded expression the search compares with word_similarity.
            migrationBuilder.Sql("""
                CREATE INDEX "IX_catalog_products_search_trgm" ON catalog_products
                USING gin (translate(lower(name || ' ' || description), 'áéíóúü', 'aeiouu') gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Extensions stay installed: other objects (for example the PostGIS geocoder) may depend on them.
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_catalog_products_search_trgm";""");
        }
    }
}

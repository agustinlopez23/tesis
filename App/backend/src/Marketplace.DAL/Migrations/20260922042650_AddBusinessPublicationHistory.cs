using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessPublicationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "first_published_at",
                table: "businesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "publication_history_unknown",
                table: "businesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // A legacy draft may have been published previously; neither status nor
            // created_at is evidence of its first publication. Preserve that uncertainty.
            migrationBuilder.Sql("UPDATE businesses SET publication_history_unknown = TRUE");
            migrationBuilder.Sql("""
                CREATE FUNCTION preserve_business_publication_history() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        NEW.first_published_at := NULL;
                        NEW.publication_history_unknown := FALSE;
                    ELSE
                        NEW.first_published_at := OLD.first_published_at;
                        NEW.publication_history_unknown := OLD.publication_history_unknown;
                    END IF;
                    IF NEW.status = 'published' AND NEW.first_published_at IS NULL
                        AND NOT NEW.publication_history_unknown THEN
                        NEW.first_published_at := clock_timestamp();
                    END IF;
                    RETURN NEW;
                END;
                $$;
                CREATE TRIGGER businesses_publication_history
                BEFORE INSERT OR UPDATE ON businesses
                FOR EACH ROW EXECUTE FUNCTION preserve_business_publication_history();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER businesses_publication_history ON businesses;
                DROP FUNCTION preserve_business_publication_history();
                """);
            migrationBuilder.DropColumn(
                name: "first_published_at",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "publication_history_unknown",
                table: "businesses");
        }
    }
}


using FluentMigrator;

namespace Aesir.Modules.Configuration.Migrations;

[Migration(20251015120001, "Add Google Search credentials to general settings")]
public class Migration20251015120001 : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_general_settings")
            .InSchema("aesir")
            .AddColumn("google_search_engine_id").AsString().Nullable()
            .AddColumn("google_api_key").AsString().Nullable();
    }

    public override void Down()
    {
        Delete.Column("google_search_engine_id")
            .FromTable("aesir_general_settings")
            .InSchema("aesir");

        Delete.Column("google_api_key")
            .FromTable("aesir_general_settings")
            .InSchema("aesir");
    }
}

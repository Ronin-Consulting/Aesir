
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20251009140501, "Fix icons for internal tools")]
public class Migration20251009140501 : Migration
{
    public override void Up()
    {
        Execute.Sql("UPDATE aesir.aesir_tool SET icon_name = 'Paperclip' WHERE name = 'RAG'");
        Execute.Sql("UPDATE aesir.aesir_tool SET icon_name = 'World' WHERE name = 'Web'");
    }

    public override void Down()
    {

    }
}
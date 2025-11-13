using FluentMigrator;

namespace Aesir.Modules.Mcp.Migrations;

[Migration(20250821112601, "Fixed existing rows HTTP Headers")]
public class Migration20250821112601 : Migration
{
    public override void Up()
    {   
        Update.Table("aesir_mcp_server")
            .InSchema("aesir")
            .Set(new { http_headers = "{}" })
            .AllRows();
    }

    public override void Down()
    {
    }
}
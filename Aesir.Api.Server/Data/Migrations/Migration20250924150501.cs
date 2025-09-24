
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250924150501, "Rename mcp_server_tool_name column to tool_name in aesir_tool table")]
public class Migration20250924150501 : Migration
{
    public override void Up()
    {
        Rename.Column("mcp_server_tool_name")
            .OnTable("aesir_tool")
            .InSchema("aesir")
            .To("tool_name");
    }

    public override void Down()
    {
        Rename.Column("tool_name")
            .OnTable("aesir_tool")
            .InSchema("aesir")
            .To("mcp_server_tool_name");
    }
}
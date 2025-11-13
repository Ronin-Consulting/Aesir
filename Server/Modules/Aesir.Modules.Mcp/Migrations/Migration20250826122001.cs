using FluentMigrator;

namespace Aesir.Modules.Mcp.Migrations;

[Migration(20250826122001, "Added Tool relationship to MCP Server")]
public class Migration20250826122001 : Migration
{
    public override void Up()
    {   
        Alter.Table("aesir_tool")
            .InSchema("aesir")
            .AddColumn("mcp_server_id").AsGuid().Nullable()
            .AddColumn("mcp_server_tool_name").AsString().Nullable();

        Create.ForeignKey("FK_aesir_tool_mcp_server_id_aesir_mcp_server_id")
            .FromTable("aesir_tool").InSchema("aesir").ForeignColumn("mcp_server_id")
            .ToTable("aesir_mcp_server").InSchema("aesir").PrimaryColumn("id");
    }

    public override void Down()
    {
    }
}
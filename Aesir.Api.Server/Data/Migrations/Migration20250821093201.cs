using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250821093201, "Added MCP Server fields location, url, headers")]
public class Migration20250821093201 : Migration
{
    public override void Up()
    {
        Alter.Column("command")
            .OnTable("aesir_mcp_server")
            .InSchema("aesir")
            .AsString().Nullable();
        
        Alter.Table("aesir_mcp_server")
            .InSchema("aesir")
            .AddColumn("location").AsInt16().Nullable()
            .AddColumn("url").AsString().Nullable()
            .AddColumn("http_headers").AsCustom("jsonb").Nullable();
        
        Update.Table("aesir_mcp_server")
            .InSchema("aesir")
            .Set(new { location = 0 })
            .AllRows();
        
        Alter.Column("location")
            .OnTable("aesir_mcp_server")
            .InSchema("aesir")
            .AsInt16().NotNullable();
        
        // JSON structure validation constraints
        Execute.Sql(@"
            ALTER TABLE aesir.aesir_mcp_server 
            ADD CONSTRAINT check_http_headers_is_object 
            CHECK (http_headers IS NULL OR jsonb_typeof(http_headers) = 'object');
        ");
    }

    public override void Down()
    {
    }
}
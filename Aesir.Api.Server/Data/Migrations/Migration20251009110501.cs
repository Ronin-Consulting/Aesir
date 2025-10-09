
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20251009110501, "Update tool columns for tool icon")]
public class Migration20251009110501 : Migration
{
    public override void Up()
    {
        // Add tool_name column to aesir_tool
        Alter.Table("aesir_tool")
            .InSchema("aesir")
            .AddColumn("icon_name").AsString().Nullable();
        
        // set tool name of any existing tools
        Execute.Sql("UPDATE aesir.aesir_tool SET icon_name = 'Server'");
        
        Alter.Table("aesir_tool")
            .InSchema("aesir")
            .AlterColumn("icon_name").AsString().NotNullable();
    }

    public override void Down()
    {

    }
}
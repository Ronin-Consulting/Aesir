
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250926055201, "")]
public class Migration20250926055201 : Migration
{
    public override void Up()
    {
        Create
            .Column("allow_thinking")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsBoolean()
            .Nullable();
        
        Create
            .Column("think_value")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsString()
            .Nullable();
    }

    public override void Down()
    {

    }
}
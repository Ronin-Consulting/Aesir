using FluentMigrator;

namespace Aesir.Modules.Inference.Migrations;

[Migration(20250813111901, "Changed descriptons to text")]
public class Migration20250813111901 : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_agent")
            .InSchema("aesir")
            .AlterColumn("description").AsString().Nullable();
        
        Alter.Table("aesir_tool")
            .InSchema("aesir")
            .AlterColumn("description").AsString().Nullable();
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
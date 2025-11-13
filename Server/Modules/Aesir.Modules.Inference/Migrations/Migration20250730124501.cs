using FluentMigrator;

namespace Aesir.Modules.Inference.Migrations;

[Migration(20250730124501, "Rename table")]
public class Migration20250730124501 : Migration
{
    public override void Up()
    {
        Rename.Table("aesir_agent_tools")
            .InSchema("aesir")
            .To("aesir_agent_tool");
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
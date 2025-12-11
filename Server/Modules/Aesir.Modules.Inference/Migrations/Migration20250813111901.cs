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
        // Column type changes may cause data truncation - manual intervention required
        throw new NotSupportedException(
            "Migration rollback is not supported. Reverting column type changes may cause data loss. " +
            "Manual database intervention is required if rollback is necessary.");
    }
}
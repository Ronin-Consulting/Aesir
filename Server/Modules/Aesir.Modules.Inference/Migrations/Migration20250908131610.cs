
using FluentMigrator;

namespace Aesir.Modules.Inference.Migrations;

[Migration(20250908131610, "Update agent columns for custom prompt")]
public class Migration20250908131610 : Migration
{
    public override void Up()
    {
        Rename.Column("prompt")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .To("prompt_persona");

        Create.Column("custom_prompt_content")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsString()
            .Nullable();
    }

    public override void Down()
    {
    }
}
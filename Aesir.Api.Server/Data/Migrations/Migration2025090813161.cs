
using FluentMigrator;
using ReverseMarkdown.Converters;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(2025090813161, "Update agent columns for custom ptomp")]
public class Migration2025090813161 : Migration
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
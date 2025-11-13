
using FluentMigrator;

namespace Aesir.Modules.Inference.Migrations;

[Migration(20250911101001, "Added new agent fields and normalized names")]
public class Migration20250911101001 : Migration
{
    public override void Up()
    {
        Create
            .Column("chat_max_tokens")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsInt32()
            .Nullable();
        Create
            .Column("chat_temperature")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsDouble()
            .Nullable();
        Create
            .Column("chat_top_p")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsDouble()
            .Nullable();
        Rename.Column("prompt_persona")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .To("chat_prompt_persona");
        Rename.Column("custom_prompt_content")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .To("chat_custom_prompt_content");
    }

    public override void Down()
    {
    }
}
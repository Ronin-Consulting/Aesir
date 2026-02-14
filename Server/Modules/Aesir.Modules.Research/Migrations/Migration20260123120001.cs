using FluentMigrator;

namespace Aesir.Modules.Research.Migrations;

/// <summary>
/// Adds column to track whether the original chat session title should be preserved.
/// When research is started from an existing chat session, this flag is set to true
/// to prevent the title from being overwritten with the synthesized report title.
/// </summary>
[Migration(20260123120001)]
public class AddPreserveOriginalChatTitleColumn : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_research_session").InSchema("aesir")
            .AddColumn("preserve_original_chat_title").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("preserve_original_chat_title").FromTable("aesir_research_session").InSchema("aesir");
    }
}

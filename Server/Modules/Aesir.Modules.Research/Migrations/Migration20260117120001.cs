using FluentMigrator;

namespace Aesir.Modules.Research.Migrations;

/// <summary>
/// Renames conversation_id to chat_session_id for clarity.
/// The column stores the aesir_chat_session.id (primary key), not a conversation ID.
/// </summary>
[Migration(20260117120001)]
public class RenameConversationIdToChatSessionId : Migration
{
    public override void Up()
    {
        // Rename the column
        Rename.Column("conversation_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir")
            .To("chat_session_id");

        // Drop the old index
        Delete.Index("ix_aesir_research_session_conversation_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir");

        // Create the new index with the correct name
        Create.Index("ix_aesir_research_session_chat_session_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir")
            .OnColumn("chat_session_id");
    }

    public override void Down()
    {
        // Rename back to original
        Rename.Column("chat_session_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir")
            .To("conversation_id");

        // Drop the new index
        Delete.Index("ix_aesir_research_session_chat_session_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir");

        // Recreate the old index
        Create.Index("ix_aesir_research_session_conversation_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir")
            .OnColumn("conversation_id");
    }
}

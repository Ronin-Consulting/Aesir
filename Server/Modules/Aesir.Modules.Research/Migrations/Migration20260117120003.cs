using FluentMigrator;

namespace Aesir.Modules.Research.Migrations;

/// <summary>
/// Adds CASCADE foreign key constraint from aesir_research_session.chat_session_id
/// to aesir_chat_session.id.
///
/// CASCADE BEHAVIOR:
/// When a chat session is deleted, all linked research sessions are automatically deleted.
/// The existing cascade chain then deletes all child records:
///   - aesir_research_submission (via session_id FK)
///   - aesir_research_peer_review (via session_id and submission_id FKs)
///   - aesir_research_report (via session_id FK)
///
/// This ensures complete cleanup of all research data when the parent chat session is removed.
///
/// NULLABLE FK:
/// The chat_session_id column is nullable - research sessions can exist independently
/// without a linked chat session. NULL values are ignored by the CASCADE constraint.
/// This supports the future workflow where research can run as a standalone process.
/// </summary>
[Migration(20260117120003)]
public class AddChatSessionForeignKey : Migration
{
    public override void Up()
    {
        // Clean up orphaned data: Set chat_session_id to NULL for any research sessions
        // that reference non-existent chat sessions (data integrity check before adding FK)
        Execute.Sql(@"
            UPDATE aesir.aesir_research_session
            SET chat_session_id = NULL
            WHERE chat_session_id IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM aesir.aesir_chat_session
                  WHERE id = aesir_research_session.chat_session_id
              )
        ");

        // Add foreign key constraint with CASCADE delete
        // CASCADE CHAIN: chat_session → research_session → submissions/reviews/reports
        // When chat session is deleted, all linked research sessions and their children
        // are automatically deleted by PostgreSQL in a single atomic operation.
        Create.ForeignKey("FK_aesir_research_session_chat_session_id_aesir_chat_session_id")
            .FromTable("aesir_research_session").InSchema("aesir").ForeignColumn("chat_session_id")
            .ToTable("aesir_chat_session").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        // Remove foreign key constraint
        // Note: This does NOT restore orphaned data that was nullified in Up()
        Delete.ForeignKey("FK_aesir_research_session_chat_session_id_aesir_chat_session_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir");
    }
}

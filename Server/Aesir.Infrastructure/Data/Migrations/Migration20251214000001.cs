using FluentMigrator;

namespace Aesir.Infrastructure.Data.Migrations;

[Migration(20251214000001, "Fix full-text search index JSON path filter operator")]
public class Migration20251214000001 : Migration
{
    public override void Up()
    {
        // Drop the existing index (may have incorrect filter operator)
        Execute.Sql("DROP INDEX IF EXISTS aesir.idx_aesir_chat_session_messages_text_search;");

        // Recreate with correct PascalCase JSON property names and <> operator for filter
        Execute.Sql(@"CREATE INDEX idx_aesir_chat_session_messages_text_search
                     ON aesir.aesir_chat_session
                     USING GIN (to_tsvector('english',
                                COALESCE(jsonb_path_query_array(conversation, '$.Messages[*] ? (@.Role <> ""system"").Content')::text, '')));");
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS aesir.idx_aesir_chat_session_messages_text_search;");
        Execute.Sql(@"CREATE INDEX idx_aesir_chat_session_messages_text_search
                     ON aesir.aesir_chat_session
                     USING GIN (to_tsvector('english',
                                jsonb_path_query_array(conversation, '$.Messages[*] ? (@.Role != ""system"").Content')::text));");
    }
}

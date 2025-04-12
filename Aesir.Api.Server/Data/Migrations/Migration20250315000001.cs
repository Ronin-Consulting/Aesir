using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250315000001, "Add full-text search capabilities to aesir_chat_session table")]
public class Migration20250315000001 : Migration
{
    public override void Up()
    {
        // Ensure the pg_trgm extension is enabled (for similarity searches)
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        
        // Create a GIN index on the title for full-text search
        Execute.Sql(@"CREATE INDEX idx_aesir_chat_session_title_search 
                     ON aesir.aesir_chat_session 
                     USING GIN (to_tsvector('english', title));");
        
        // Optionally, for text search within the Messages array
        Execute.Sql(@"CREATE INDEX idx_aesir_chat_session_messages_text_search 
                     ON aesir.aesir_chat_session 
                     USING GIN (to_tsvector('english', 
                                jsonb_path_query_array(conversation, '$.Messages[*] ? (@.Role != ""system"").Content')::text));");
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS aesir.idx_aesir_chat_session_messages_text_search;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.idx_aesir_chat_session_title_search;");
    }
}
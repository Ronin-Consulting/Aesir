using FluentMigrator;

namespace Aesir.Modules.Logging.Migrations;

[Migration(20251214150000, "Replace kernel log table with inference log table")]
public class Migration20251214150000 : Migration
{
    public override void Up()
    {
        // Drop the old kernel log table (indexes are dropped automatically with the table)
        Execute.Sql("DROP TABLE IF EXISTS aesir.aesir_log_kernel CASCADE;");

        // Create the new inference log table
        Execute.Sql(@"
            CREATE TABLE aesir.aesir_log_inference (
                id UUID PRIMARY KEY,
                chat_session_id UUID,
                conversation_id UUID,
                user_query TEXT NOT NULL,
                user_query_truncated VARCHAR(500),
                assistant_response VARCHAR(500),
                tool_calls JSONB,
                tool_call_count INT NOT NULL DEFAULT 0,
                total_duration_ms BIGINT,
                started_at TIMESTAMPTZ NOT NULL,
                completed_at TIMESTAMPTZ,
                status VARCHAR(20) NOT NULL,
                error_message VARCHAR(1000)
            );
        ");

        // Index on chat_session_id for filtering by session
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_chat_session_id
            ON aesir.aesir_log_inference (chat_session_id);
        ");

        // Index on conversation_id for filtering by conversation
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_conversation_id
            ON aesir.aesir_log_inference (conversation_id);
        ");

        // Index on started_at for time range queries and sorting (most recent first)
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_started_at
            ON aesir.aesir_log_inference (started_at DESC);
        ");

        // Index on status for status filtering
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_status
            ON aesir.aesir_log_inference (status);
        ");

        // GIN index on tool_calls JSONB for efficient nested queries
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_tool_calls_gin
            ON aesir.aesir_log_inference USING GIN (tool_calls);
        ");

        // Index on tool_call_count for filtering by number of tool calls
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_tool_call_count
            ON aesir.aesir_log_inference (tool_call_count);
        ");

        // Composite index for common query patterns (status + time)
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_status_started
            ON aesir.aesir_log_inference (status, started_at DESC);
        ");

        // Text search index on user_query_truncated for search functionality
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_inference_user_query_truncated
            ON aesir.aesir_log_inference (user_query_truncated varchar_pattern_ops);
        ");
    }

    public override void Down()
    {
        // Drop the new inference log table
        Execute.Sql("DROP TABLE IF EXISTS aesir.aesir_log_inference CASCADE;");

        // Recreate the old kernel log table (from Migration20250924131101)
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS aesir.aesir_log_kernel (
                id UUID PRIMARY KEY,
                level VARCHAR(12) NOT NULL,
                message VARCHAR(1000) NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                details JSONB
            );
        ");

        // Recreate indexes from Migration20251214120000
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_created_at ON aesir.aesir_log_kernel (created_at DESC);");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_level ON aesir.aesir_log_kernel (level);");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_details_gin ON aesir.aesir_log_kernel USING GIN (details);");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_chat_session_id ON aesir.aesir_log_kernel ((details->>'ChatSessionId'));");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_conversation_id ON aesir.aesir_log_kernel ((details->>'ConversationId'));");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_type ON aesir.aesir_log_kernel ((details->>'Type'));");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_function_name ON aesir.aesir_log_kernel ((details->>'FunctionName') varchar_pattern_ops);");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_plugin_name ON aesir.aesir_log_kernel ((details->>'PluginName') varchar_pattern_ops);");
        Execute.Sql(@"CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_level_created ON aesir.aesir_log_kernel (level, created_at DESC);");
    }
}

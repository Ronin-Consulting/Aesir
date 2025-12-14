using FluentMigrator;

namespace Aesir.Modules.Logging.Migrations;

[Migration(20251214120000, "Add indexes for observability queries")]
public class Migration20251214120000 : Migration
{
    public override void Up()
    {
        // Index on created_at for time range queries and sorting
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_created_at
            ON aesir.aesir_log_kernel (created_at DESC);
        ");

        // Index on level for log level filtering
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_level
            ON aesir.aesir_log_kernel (level);
        ");

        // GIN index on details JSONB for efficient JSONB queries
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_details_gin
            ON aesir.aesir_log_kernel USING GIN (details);
        ");

        // Specific BTREE indexes on commonly queried JSONB fields (using PascalCase keys as stored by Newtonsoft.Json)
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_chat_session_id
            ON aesir.aesir_log_kernel ((details->>'ChatSessionId'));
        ");

        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_conversation_id
            ON aesir.aesir_log_kernel ((details->>'ConversationId'));
        ");

        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_type
            ON aesir.aesir_log_kernel ((details->>'Type'));
        ");

        // Text search index on FunctionName for partial matching (using PascalCase keys as stored by Newtonsoft.Json)
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_function_name
            ON aesir.aesir_log_kernel ((details->>'FunctionName') varchar_pattern_ops);
        ");

        // Text search index on PluginName for partial matching (using PascalCase keys as stored by Newtonsoft.Json)
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_plugin_name
            ON aesir.aesir_log_kernel ((details->>'PluginName') varchar_pattern_ops);
        ");

        // Composite index for common query patterns
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_aesir_log_kernel_level_created
            ON aesir.aesir_log_kernel (level, created_at DESC);
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_created_at;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_level;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_details_gin;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_chat_session_id;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_conversation_id;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_type;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_function_name;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_plugin_name;");
        Execute.Sql("DROP INDEX IF EXISTS aesir.ix_aesir_log_kernel_level_created;");
    }
}

using FluentMigrator;

namespace Aesir.Modules.Logging.Migrations;

[Migration(20251215120000, "Add document operation log table")]
public class Migration20251215120000 : Migration
{
    public override void Up()
    {
        // Create the document log table
        Execute.Sql(@"
            CREATE TABLE aesir.aesir_log_document (
                id UUID PRIMARY KEY,
                chat_session_id UUID,
                conversation_id UUID,
                operation_type VARCHAR(30) NOT NULL,
                file_name VARCHAR(500) NOT NULL,
                file_path VARCHAR(1000),
                file_size_bytes BIGINT,
                content_type VARCHAR(100),
                chunk_count INT,
                embedding_count INT,
                token_count INT,
                processing_duration_ms BIGINT,
                started_at TIMESTAMPTZ NOT NULL,
                completed_at TIMESTAMPTZ,
                status VARCHAR(20) NOT NULL,
                error_message VARCHAR(1000),
                metadata JSONB
            );
        ");

        // Index on started_at for time range queries and sorting (most recent first)
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_started_at
            ON aesir.aesir_log_document (started_at DESC);
        ");

        // Index on chat_session_id for filtering by session
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_chat_session_id
            ON aesir.aesir_log_document (chat_session_id);
        ");

        // Index on conversation_id for filtering by conversation
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_conversation_id
            ON aesir.aesir_log_document (conversation_id);
        ");

        // Index on status for status filtering
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_status
            ON aesir.aesir_log_document (status);
        ");

        // Index on operation_type for filtering by operation
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_operation_type
            ON aesir.aesir_log_document (operation_type);
        ");

        // Composite index for common query patterns (status + time)
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_status_started
            ON aesir.aesir_log_document (status, started_at DESC);
        ");

        // Composite index for operation_type + time
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_operation_started
            ON aesir.aesir_log_document (operation_type, started_at DESC);
        ");

        // GIN index on metadata JSONB for flexible queries
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_metadata_gin
            ON aesir.aesir_log_document USING GIN (metadata);
        ");

        // Text search index on file_name for search functionality
        Execute.Sql(@"
            CREATE INDEX ix_aesir_log_document_file_name
            ON aesir.aesir_log_document (file_name varchar_pattern_ops);
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS aesir.aesir_log_document CASCADE;");
    }
}

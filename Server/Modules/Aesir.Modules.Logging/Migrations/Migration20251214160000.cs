using FluentMigrator;

namespace Aesir.Modules.Logging.Migrations;

/// <summary>
/// Adds assistant_response_full column to store complete AI responses.
/// The existing assistant_response column (VARCHAR 500) remains as the truncated preview.
/// </summary>
[Migration(20251214160000, "Add full assistant response column")]
public class Migration20251214160000 : Migration
{
    public override void Up()
    {
        // Add column for full assistant response (TEXT for unlimited length)
        Execute.Sql(@"
            ALTER TABLE aesir.aesir_log_inference
            ADD COLUMN IF NOT EXISTS assistant_response_full TEXT;
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"
            ALTER TABLE aesir.aesir_log_inference
            DROP COLUMN IF EXISTS assistant_response_full;
        ");
    }
}

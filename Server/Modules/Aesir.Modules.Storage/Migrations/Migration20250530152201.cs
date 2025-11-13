using FluentMigrator;

namespace Aesir.Modules.Storage.Migrations;

[Migration(20250530152201, "Create file storage table for managing uploaded files")]
public class Migration20250530152201 : Migration
{
    public override void Up()
    {
        Execute.Sql("""
           CREATE TABLE aesir.aesir_file_storage (
               id UUID DEFAULT UUID_GENERATE_V4() NOT NULL,
               file_name VARCHAR(255) NOT NULL,
               mime_type VARCHAR(100) NOT NULL,
               file_size BIGINT NOT NULL,
               file_content BYTEA NOT NULL, -- MAX size 1GB
               created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
               updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
           );
       """);

        Create.UniqueConstraint("uc_file_name")
            .OnTable("aesir_file_storage")
            .WithSchema("aesir")
            .Column("file_name");
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
using FluentMigrator;

namespace Aesir.Modules.Storage.Migrations;

[Migration(20251210120001, "Add thumbnail columns to file storage table")]
public class Migration20251210120001 : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_file_storage")
            .InSchema("aesir")
            .AddColumn("thumbnail_content").AsBinary().Nullable()
            .AddColumn("thumbnail_mime_type").AsString(50).Nullable()
            .AddColumn("has_thumbnail").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("thumbnail_content").FromTable("aesir_file_storage").InSchema("aesir");
        Delete.Column("thumbnail_mime_type").FromTable("aesir_file_storage").InSchema("aesir");
        Delete.Column("has_thumbnail").FromTable("aesir_file_storage").InSchema("aesir");
    }
}

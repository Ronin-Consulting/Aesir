
using FluentMigrator;

namespace Aesir.Modules.Logging.Migrations;

[Migration(20250924131101, "Add the kernel logging table")]
public class Migration20250924131101 : Migration
{
    public override void Up()
    {
        Create
            .Table("aesir_log_kernel")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("level").AsString(12).NotNullable()
            .WithColumn("message").AsString(1000).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("details").AsCustom("JSONB").NotNullable();
    }

    public override void Down()
    {
    }
}
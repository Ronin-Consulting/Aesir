using FluentMigrator;

namespace Aesir.Modules.Projects.Migrations;

[Migration(20260103120001, "Create aesir_project table")]
public class Migration20260103120001 : Migration
{
    public override void Up()
    {
        Create.Table("aesir_project")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("user_id").AsString(255).NotNullable()
            .WithColumn("name").AsString(255).NotNullable()
            .WithColumn("description").AsString().Nullable()
            .WithColumn("instructions").AsString().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("is_deleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
            .WithColumn("deleted_by").AsString(255).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("created_by").AsString(255).NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_by").AsString(255).Nullable();

        // Unique constraint on name (globally unique)
        Create.Index("ix_aesir_project_name")
            .OnTable("aesir_project")
            .InSchema("aesir")
            .OnColumn("name")
            .Unique();

        // Index for user lookup
        Create.Index("ix_aesir_project_user_id")
            .OnTable("aesir_project")
            .InSchema("aesir")
            .OnColumn("user_id");

        // Index for active/deleted filtering
        Create.Index("ix_aesir_project_is_deleted")
            .OnTable("aesir_project")
            .InSchema("aesir")
            .OnColumn("is_deleted");
    }

    public override void Down()
    {
        Delete.Table("aesir_project").InSchema("aesir");
    }
}

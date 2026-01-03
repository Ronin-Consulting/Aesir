using FluentMigrator;

namespace Aesir.Modules.Projects.Migrations;

[Migration(20260103120002, "Add 'project_id' column to the 'aesir_chat_session' table")]
public class Migration20260103120002 : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_chat_session")
            .InSchema("aesir")
            .AddColumn("project_id").AsGuid().Nullable();

        Create.Index("ix_aesir_chat_session_project_id")
            .OnTable("aesir_chat_session")
            .InSchema("aesir")
            .OnColumn("project_id");

        Create.ForeignKey("FK_aesir_chat_session_project_id_aesir_project_id")
            .FromTable("aesir_chat_session").InSchema("aesir").ForeignColumn("project_id")
            .ToTable("aesir_project").InSchema("aesir").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.SetNull);
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_aesir_chat_session_project_id_aesir_project_id")
            .OnTable("aesir_chat_session")
            .InSchema("aesir");

        Delete.Index("ix_aesir_chat_session_project_id")
            .OnTable("aesir_chat_session")
            .InSchema("aesir");

        Delete.Column("project_id")
            .FromTable("aesir_chat_session")
            .InSchema("aesir");
    }
}

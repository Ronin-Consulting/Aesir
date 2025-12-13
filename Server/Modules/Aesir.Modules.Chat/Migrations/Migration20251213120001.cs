using FluentMigrator;

namespace Aesir.Modules.Chat.Migrations;

[Migration(20251213120001, "Add 'is_starred' column to the 'aesir_chat_session' table")]
public class Migration20251213120001 : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_chat_session")
            .InSchema("aesir")
            .AddColumn("is_starred").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("is_starred")
            .FromTable("aesir_chat_session")
            .InSchema("aesir");
    }
}

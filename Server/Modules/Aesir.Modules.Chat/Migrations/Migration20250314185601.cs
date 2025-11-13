using FluentMigrator;

namespace Aesir.Modules.Chat.Migrations;

[Migration(20250314185601, "Add a 'title' column to the 'aesir_chat_session' table in the 'aesir' schema")]
public class Migration20250314185601 : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_chat_session")
            .InSchema("aesir")
            .AddColumn("title").AsString().NotNullable().WithDefaultValue("Default Title");
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
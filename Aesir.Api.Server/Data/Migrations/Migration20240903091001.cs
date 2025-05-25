using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20240903091001, "Creates the 'aesir' schema, sets its ownership and access, ensures 'uuid-ossp' extension is enabled, and defines the 'aesir_chat_session' table.")]
public class Migration20240903091001 : Migration
{
    public override void Up()
    {
        // create schema
        Create.Schema("aesir");
        Execute.Sql(@"alter schema aesir owner to pg_database_owner");
        Execute.Sql(@"grant usage on schema aesir to public");
        Execute.Sql(@"CREATE EXTENSION IF NOT EXISTS ""uuid-ossp""");

        Create.Table("aesir_chat_session")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
                .WithDefault(SystemMethods.NewGuid)
            .WithColumn("user_id").AsString().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable()
            .WithColumn("conversation").AsCustom("JSONB").NotNullable();
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
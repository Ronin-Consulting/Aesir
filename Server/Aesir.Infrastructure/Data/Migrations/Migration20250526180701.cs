using FluentMigrator;

namespace Aesir.Infrastructure.Data.Migrations;

[Migration(20250526180701, "Add vector extension")]
public class Migration20250526180701 : Migration
{
    public override void Up()
    {
        // Ensure vector extension is installed
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
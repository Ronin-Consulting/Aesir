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
        // Vector extension may be used by other tables - dropping could break the database
        throw new NotSupportedException(
            "Migration rollback is not supported. The vector extension may be in use by other tables. " +
            "Manual database intervention is required if rollback is necessary.");
    }
}
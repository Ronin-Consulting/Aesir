using FluentMigrator;

namespace Aesir.Modules.Inference.Migrations;

[Migration(20250813100501, "Add columns to tool table, add description to agent")]
public class Migration20250813100501 : Migration
{
    public override void Up()
    {
        Alter.Table("aesir_agent")
            .InSchema("aesir")
            .AddColumn("description").AsString(500).Nullable();

        Alter.Table("aesir_tool")
            .InSchema("aesir")
            .AddColumn("description").AsString(500).Nullable()
            .AddColumn("type").AsInt16().Nullable();
        
        Update.Table("aesir_tool")
            .InSchema("aesir")
            .Set(new { type = 0 })
            .AllRows();
        
        Update.Table("aesir_tool")
            .InSchema("aesir")
            .Set(new { description = "Supports searching documents with Retrieval Augmented Generation" })
            .Where(new { name = "RAG" });
        
        Update.Table("aesir_tool")
            .InSchema("aesir")
            .Set(new { description = "Provides the ability to search the web using Google" })
            .Where(new { name = "Web" });
        
        Alter.Column("type")
            .OnTable("aesir_tool")
            .InSchema("aesir")
            .AsInt16().NotNullable();
        
        
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
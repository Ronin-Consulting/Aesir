using FluentMigrator;

namespace Aesir.Modules.Inference.Migrations;

[Migration(20250724165701, "Add the 'aesir_agent' table in the 'aesir' schema")]
public class Migration20250724165701 : Migration
{
    public override void Up()
    {
        Create.Table("aesir_agent")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("chat_model").AsString().NotNullable()
            .WithColumn("embedding_model").AsString().NotNullable()
            .WithColumn("vision_model").AsString().NotNullable()
            .WithColumn("source").AsInt16().NotNullable()
            .WithColumn("prompt").AsInt16().NotNullable();

        Create.Table("aesir_tool")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("name").AsString().NotNullable();

        Create.Table("aesir_agent_tools")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("agent_id").AsGuid().NotNullable()
            .WithColumn("tool_id").AsGuid().NotNullable();

        // Add foreign key constraints separately with explicit schema references
        Create.ForeignKey("FK_aesir_agent_tools_agent_id_aesir_agent_id")
            .FromTable("aesir_agent_tools").InSchema("aesir").ForeignColumn("agent_id")
            .ToTable("aesir_agent").InSchema("aesir").PrimaryColumn("id");

        Create.ForeignKey("FK_aesir_agent_tools_tool_id_aesir_tool_id")
            .FromTable("aesir_agent_tools").InSchema("aesir").ForeignColumn("tool_id")
            .ToTable("aesir_tool").InSchema("aesir").PrimaryColumn("id");

    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
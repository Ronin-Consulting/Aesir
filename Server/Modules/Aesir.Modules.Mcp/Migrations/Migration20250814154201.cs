using FluentMigrator;

namespace Aesir.Modules.Mcp.Migrations;

[Migration(20250814154201, "Added MCP Server table")]
public class Migration20250814154201 : Migration
{
    public override void Up()
    {
        Create.Table("aesir_mcp_server")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("description").AsString().Nullable()
            .WithColumn("command").AsString().NotNullable()
            .WithColumn("arguments").AsCustom("jsonb").Nullable()
            .WithColumn("environment_variables").AsCustom("jsonb").Nullable();
        
        // JSON structure validation constraints
        Execute.Sql(@"
            ALTER TABLE aesir.aesir_mcp_server 
            ADD CONSTRAINT check_arguments_is_array 
            CHECK (arguments IS NULL OR jsonb_typeof(arguments) = 'array'),
            ADD CONSTRAINT check_env_vars_is_object 
            CHECK (environment_variables IS NULL OR jsonb_typeof(environment_variables) = 'object');
        ");

    }

    public override void Down()
    {
        Delete.Table("aesir_mcp_server").InSchema("aesir");
    }
}
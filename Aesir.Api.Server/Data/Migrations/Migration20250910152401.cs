
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250910152401, "Fixed oops, removed wrong columns")]
public class Migration20250910152401 : Migration
{
    public override void Up()
    {
        Delete
            .FromTable("aesir_agent")
            .InSchema("aesir")
            .AllRows();
            
        Delete
            .Column("vision_inference_engine_id")
            .FromTable("aesir_agent")
            .InSchema("aesir");
        
        Delete
            .Column("vision_model")
            .FromTable("aesir_agent")
            .InSchema("aesir");

        Create
            .Column("chat_inference_engine_id")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsGuid()
            .NotNullable();

        Create
            .Column("chat_model")
            .OnTable("aesir_agent")
            .InSchema("aesir")
            .AsString()
            .NotNullable();

        // Add foreign key constraint for chat_inference_engine_id
        Create.ForeignKey("FK_aesir_agent_chat_inference_engine_id_aesir_inference_engine_id")
            .FromTable("aesir_agent").InSchema("aesir").ForeignColumn("chat_inference_engine_id")
            .ToTable("aesir_inference_engine").InSchema("aesir").PrimaryColumn("id");
    }

    public override void Down()
    {
    }
}
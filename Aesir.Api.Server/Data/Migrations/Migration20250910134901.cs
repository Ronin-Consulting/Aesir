
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250910134901, "Moved location of vision model")]
public class Migration20250910134901 : Migration
{
    public override void Up()
    {
        Delete
            .Column("chat_inference_engine_id")
            .FromTable("aesir_agent")
            .InSchema("aesir");
        
        Delete
            .Column("chat_model")
            .FromTable("aesir_agent")
            .InSchema("aesir");
        
        Rename
            .Column("rag_inf_eng_id")
            .OnTable("aesir_general_settings")
            .InSchema("aesir")
            .To("rag_emb_inf_eng_id");

        Create
            .Column("rag_vis_inf_eng_id")
            .OnTable("aesir_general_settings")
            .InSchema("aesir")
            .AsGuid()
            .Nullable();

        Create
            .Column("rag_vis_model")
            .OnTable("aesir_general_settings")
            .InSchema("aesir")
            .AsString()
            .Nullable();
        
        // foreign key
        Create.ForeignKey("FK_AppSettings_InferenceEngine_Vision")
            .FromTable("aesir_general_settings").InSchema("aesir").ForeignColumn("rag_vis_inf_eng_id")
            .ToTable("aesir_inference_engine").InSchema("aesir").PrimaryColumn("id");
    }

    public override void Down()
    {
    }
}
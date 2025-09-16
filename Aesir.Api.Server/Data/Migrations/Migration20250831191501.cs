
using FluentMigrator;
using ReverseMarkdown.Converters;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250831191501, "Fixed general setting to have id")]
public class Migration20250831191501 : Migration
{
    public override void Up()
    {
        // Recreate the aesir_general_setting table 
        Delete.Table("aesir_general_setting").InSchema("aesir");
        
        // Possibly split these out into proper tables in the future as needed
        Create.Table("aesir_general_setting")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("value").AsString().Nullable()
            .WithColumn("description").AsString().Nullable();

        // Add unique constraint to aesir_general_setting.name
        Create.UniqueConstraint("UQ_aesir_general_setting_name")
            .OnTable("aesir_general_setting")
            .WithSchema("aesir")
            .Column("name");
        
        Insert.IntoTable("aesir_general_setting")
            .InSchema("aesir")
            .Row(new
            {
                name = "rag_inf_eng_id", description = "Inference engine id for the source of the RAG embedding model"
            })
            .Row(new
            {
                name = "rag_emb_model", description = "RAG embedding mode id"
            })
            .Row(new
            {
                name = "tts_model", value = "Assets/vits-piper-en_US-joe-medium/en_US-joe-medium.onnx", description = "Text to Speech Model"
            })
            .Row(new
            {
                name = "stt_model", value = "Assets/whisper/ggml-base.bin", description = "Speech to Text Model"
            })
            .Row(new
            {
                name = "vad_model", value = "Assets/vad/silero_vad.onnx", description = "Voice Activity Detection Model"
            });
    }

    public override void Down()
    {
    }
}
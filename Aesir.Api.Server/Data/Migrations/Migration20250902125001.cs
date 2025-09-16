
using FluentMigrator;
using ReverseMarkdown.Converters;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250902125001, "Changed general setting to first class fields (instead of table of key values) so we can use referential integrity")]
public class Migration20250902125001 : Migration
{
    public override void Up()
    {
        // remove old table
        Delete.Table("aesir_general_setting")
            .InSchema("aesir");
        
        // define new table
        Create.Table("aesir_general_settings")
            .InSchema("aesir")
            .WithColumn("id").AsInt32().PrimaryKey().WithDefaultValue(1)
            .WithColumn("rag_inf_eng_id").AsGuid().Nullable()
            .WithColumn("rag_emb_model").AsString().Nullable()
            .WithColumn("tts_model").AsString().Nullable()
            .WithColumn("stt_model").AsString().Nullable()
            .WithColumn("vad_model").AsString().Nullable();
        
        // foreign key
        Create.ForeignKey("FK_AppSettings_Currency")
            .FromTable("aesir_general_settings").InSchema("aesir").ForeignColumn("rag_inf_eng_id")
            .ToTable("aesir_inference_engine").InSchema("aesir").PrimaryColumn("id");

        // this will make it so only ever one row can be created
        Execute.Sql("ALTER TABLE \"aesir\".\"aesir_general_settings\" ADD CONSTRAINT id_is_one CHECK (\"id\" = 1)");

        // default values
        Insert.IntoTable("aesir_general_settings")
            .InSchema("aesir")
            .Row(new
            {
                tts_model = "Assets/vits-piper-en_US-joe-medium/en_US-joe-medium.onnx",
                stt_model = "Assets/whisper/ggml-base.bin",
                vad_model = "Assets/vad/silero_vad.onnx"
            });
    }

    public override void Down()
    {
    }
}
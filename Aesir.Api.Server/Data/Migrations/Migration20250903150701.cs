
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250903150701, "Changed general setting column names")]
public class Migration20250903150701 : Migration
{
    public override void Up()
    {
        Rename.Column("tts_model")
            .OnTable("aesir_general_settings")
            .InSchema("aesir")
            .To("tts_model_path");
        Rename.Column("stt_model")
            .OnTable("aesir_general_settings")
            .InSchema("aesir")
            .To("stt_model_path");
        Rename.Column("vad_model")
            .OnTable("aesir_general_settings")
            .InSchema("aesir")
            .To("vad_model_path");
    }

    public override void Down()
    {
    }
}
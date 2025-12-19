using FluentMigrator;

namespace Aesir.Modules.Configuration.Migrations;

/// <summary>
/// Upgrades TTS model from joe-medium to lessac-high for improved voice quality.
/// </summary>
[Migration(20251218120001)]
public class Migration20251218120001 : Migration
{
    public override void Up()
    {
        // Update TTS model path to use higher quality lessac voice
        Execute.Sql(@"
            UPDATE aesir.aesir_general_settings
            SET tts_model_path = 'Assets/vits-piper-en_US-lessac-high/en_US-lessac-high.onnx'
            WHERE tts_model_path = 'Assets/vits-piper-en_US-joe-medium/en_US-joe-medium.onnx'
        ");
    }

    public override void Down()
    {
        // Revert to joe-medium model
        Execute.Sql(@"
            UPDATE aesir.aesir_general_settings
            SET tts_model_path = 'Assets/vits-piper-en_US-joe-medium/en_US-joe-medium.onnx'
            WHERE tts_model_path = 'Assets/vits-piper-en_US-lessac-high/en_US-lessac-high.onnx'
        ");
    }
}

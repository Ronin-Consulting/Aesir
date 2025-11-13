
using FluentMigrator;

namespace Aesir.Modules.Configuration.Migrations;

[Migration(20250828093901, "Added inference engine, general settings, and reworked agent to model associations")]
public class Migration20250828093901 : Migration
{
    public override void Up()
    {   
        // Create aesir_inference_engine table
        Create.Table("aesir_inference_engine")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("name").AsString().NotNullable().Unique()
            .WithColumn("description").AsString().Nullable()
            .WithColumn("type").AsInt16().NotNullable()
            .WithColumn("configuration").AsCustom("jsonb").Nullable();
        
        // JSON structure validation constraints
        Execute.Sql(@"
            ALTER TABLE aesir.aesir_inference_engine 
            ADD CONSTRAINT check_configuration_is_json 
            CHECK (configuration IS NULL OR jsonb_typeof(configuration) = 'object')");
        
        // Add unique constraint to aesir_agent.name
        Create.UniqueConstraint("UQ_aesir_agent_name")
            .OnTable("aesir_agent")
            .WithSchema("aesir")
            .Column("name");

        // Delete all agents
        Delete.FromTable("aesir_agent_tool")
            .InSchema("aesir")
            .AllRows();
        Delete.FromTable("aesir_agent")
            .InSchema("aesir")
            .AllRows();

        // Add chat_inference_engine_id column to aesir_agent
        Alter.Table("aesir_agent")
            .InSchema("aesir")
            .AddColumn("chat_inference_engine_id").AsGuid().Nullable();

        // Add foreign key constraint for chat_inference_engine_id
        Create.ForeignKey("FK_aesir_agent_chat_inference_engine_id_aesir_inference_engine_id")
            .FromTable("aesir_agent").InSchema("aesir").ForeignColumn("chat_inference_engine_id")
            .ToTable("aesir_inference_engine").InSchema("aesir").PrimaryColumn("id");
        
        // Drop source column from aesir_agent
        Delete.Column("source")
            .FromTable("aesir_agent")
            .InSchema("aesir");
        
        // Drop embedding_model column from aesir_agent
        Delete.Column("embedding_model")
            .FromTable("aesir_agent")
            .InSchema("aesir");

        // Add vision_inference_engine_id column to aesir_agent
        Alter.Table("aesir_agent")
            .InSchema("aesir")
            .AddColumn("vision_inference_engine_id").AsGuid().Nullable();

        // Add foreign key constraint for vision_inference_engine_id
        Create.ForeignKey("FK_aesir_agent_vision_inference_engine_id_aesir_inference_engine_id")
            .FromTable("aesir_agent").InSchema("aesir").ForeignColumn("vision_inference_engine_id")
            .ToTable("aesir_inference_engine").InSchema("aesir").PrimaryColumn("id");

        // Add unique constraint to aesir_tool.name
        Create.UniqueConstraint("UQ_aesir_tool_name")
            .OnTable("aesir_tool")
            .WithSchema("aesir")
            .Column("name");

        // Add unique constraint to aesir_mcp_server.name
        Create.UniqueConstraint("UQ_aesir_mcp_server_name")
            .OnTable("aesir_mcp_server")
            .WithSchema("aesir")
            .Column("name");
        
        // Possibly split these out into proper tables in the future as needed
        Create.Table("aesir_general_setting")
            .InSchema("aesir")
            .WithColumn("name").AsString().PrimaryKey()
            .WithColumn("value").AsString().Nullable()
            .WithColumn("description").AsString().Nullable();
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
        // Remove unique constraints
        Delete.UniqueConstraint("UQ_aesir_mcp_server_name")
            .FromTable("aesir_mcp_server")
            .InSchema("aesir");

        Delete.UniqueConstraint("UQ_aesir_tool_name")
            .FromTable("aesir_tool")
            .InSchema("aesir");

        // Remove foreign key constraints
        Delete.ForeignKey("FK_aesir_agent_vision_inference_engine_id_aesir_inference_engine_id")
            .OnTable("aesir_agent");

        Delete.ForeignKey("FK_aesir_agent_chat_inference_engine_id_aesir_inference_engine_id")
            .OnTable("aesir_agent");

        // Remove columns from aesir_agent
        Delete.Column("vision_inference_engine_id")
            .FromTable("aesir_agent")
            .InSchema("aesir");

        Delete.Column("chat_inference_engine_id")
            .FromTable("aesir_agent")
            .InSchema("aesir");

        // Re-add source column (assuming it was a string column)
        Alter.Table("aesir_agent")
            .InSchema("aesir")
            .AddColumn("source").AsString(255).Nullable();

        // Remove unique constraint from aesir_agent.name
        Delete.UniqueConstraint("UQ_aesir_agent_name")
            .FromTable("aesir_agent")
            .InSchema("aesir");

        // Drop aesir_inference_engine table
        Delete.Table("aesir_inference_engine")
            .InSchema("aesir");
    }
}
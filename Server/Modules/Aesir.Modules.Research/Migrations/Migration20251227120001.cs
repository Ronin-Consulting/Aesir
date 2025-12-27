using FluentMigrator;

namespace Aesir.Modules.Research.Migrations;

[Migration(20251227120001, "Create research module tables")]
public class Migration20251227120001 : Migration
{
    public override void Up()
    {
        // 1. Create aesir_research_team table
        Create.Table("aesir_research_team")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("user_id").AsString(255).NotNullable()
            .WithColumn("name").AsString(255).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.Index("ix_aesir_research_team_user_id")
            .OnTable("aesir_research_team")
            .InSchema("aesir")
            .OnColumn("user_id");

        // 2. Create aesir_research_team_member table
        Create.Table("aesir_research_team_member")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("research_team_id").AsGuid().NotNullable()
            .WithColumn("agent_id").AsGuid().NotNullable()
            .WithColumn("role").AsInt16().NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("override_temperature").AsDouble().Nullable()
            .WithColumn("override_persona").AsString().Nullable()
            .WithColumn("override_planning_prompt").AsString().Nullable()
            .WithColumn("override_research_prompt").AsString().Nullable()
            .WithColumn("override_thinking_mode").AsInt16().Nullable()
            .WithColumn("override_tools").AsCustom("jsonb").Nullable();

        Create.Index("ix_aesir_research_team_member_research_team_id")
            .OnTable("aesir_research_team_member")
            .InSchema("aesir")
            .OnColumn("research_team_id");

        Create.Index("ix_aesir_research_team_member_agent_id")
            .OnTable("aesir_research_team_member")
            .InSchema("aesir")
            .OnColumn("agent_id");

        Create.ForeignKey("FK_aesir_research_team_member_research_team_id_aesir_research_team_id")
            .FromTable("aesir_research_team_member").InSchema("aesir").ForeignColumn("research_team_id")
            .ToTable("aesir_research_team").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_aesir_research_team_member_agent_id_aesir_agent_id")
            .FromTable("aesir_research_team_member").InSchema("aesir").ForeignColumn("agent_id")
            .ToTable("aesir_agent").InSchema("aesir").PrimaryColumn("id");

        // 3. Create aesir_research_session table
        Create.Table("aesir_research_session")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("user_id").AsString(255).NotNullable()
            .WithColumn("research_team_id").AsGuid().Nullable()
            .WithColumn("conversation_id").AsGuid().Nullable()
            .WithColumn("query").AsString().NotNullable()
            .WithColumn("refined_query").AsString().Nullable()
            .WithColumn("mode").AsInt16().NotNullable()
            .WithColumn("status").AsInt16().NotNullable()
            .WithColumn("current_phase").AsInt16().Nullable()
            .WithColumn("document_collection_ids").AsCustom("jsonb").Nullable()
            .WithColumn("clarification_questions").AsCustom("jsonb").Nullable()
            .WithColumn("clarification_answers").AsCustom("jsonb").Nullable()
            .WithColumn("error_message").AsString().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable()
            .WithColumn("started_at").AsDateTimeOffset().Nullable()
            .WithColumn("completed_at").AsDateTimeOffset().Nullable();

        Create.Index("ix_aesir_research_session_user_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir")
            .OnColumn("user_id");

        Create.Index("ix_aesir_research_session_research_team_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir")
            .OnColumn("research_team_id");

        Create.Index("ix_aesir_research_session_conversation_id")
            .OnTable("aesir_research_session")
            .InSchema("aesir")
            .OnColumn("conversation_id");

        Create.ForeignKey("FK_aesir_research_session_research_team_id_aesir_research_team_id")
            .FromTable("aesir_research_session").InSchema("aesir").ForeignColumn("research_team_id")
            .ToTable("aesir_research_team").InSchema("aesir").PrimaryColumn("id");

        // 4. Create aesir_research_submission table
        Create.Table("aesir_research_submission")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("session_id").AsGuid().NotNullable()
            .WithColumn("agent_id").AsGuid().NotNullable()
            .WithColumn("role").AsInt16().NotNullable()
            .WithColumn("round_number").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("anonymized_id").AsString(10).Nullable()
            .WithColumn("plan").AsString().Nullable()
            .WithColumn("content").AsString().NotNullable()
            .WithColumn("thinking_trace").AsString().Nullable()
            .WithColumn("sources").AsCustom("jsonb").Nullable()
            .WithColumn("tool_calls").AsCustom("jsonb").Nullable()
            .WithColumn("tokens_used").AsInt32().Nullable()
            .WithColumn("duration_ms").AsInt64().Nullable()
            .WithColumn("status").AsInt16().NotNullable()
            .WithColumn("error_message").AsString().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("completed_at").AsDateTimeOffset().Nullable();

        Create.Index("ix_aesir_research_submission_session_id")
            .OnTable("aesir_research_submission")
            .InSchema("aesir")
            .OnColumn("session_id");

        Create.Index("ix_aesir_research_submission_agent_id")
            .OnTable("aesir_research_submission")
            .InSchema("aesir")
            .OnColumn("agent_id");

        Create.ForeignKey("FK_aesir_research_submission_session_id_aesir_research_session_id")
            .FromTable("aesir_research_submission").InSchema("aesir").ForeignColumn("session_id")
            .ToTable("aesir_research_session").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_aesir_research_submission_agent_id_aesir_agent_id")
            .FromTable("aesir_research_submission").InSchema("aesir").ForeignColumn("agent_id")
            .ToTable("aesir_agent").InSchema("aesir").PrimaryColumn("id");

        // 5. Create aesir_research_peer_review table
        Create.Table("aesir_research_peer_review")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("session_id").AsGuid().NotNullable()
            .WithColumn("submission_id").AsGuid().NotNullable()
            .WithColumn("reviewer_agent_id").AsGuid().NotNullable()
            .WithColumn("reviewer_role").AsInt16().NotNullable()
            .WithColumn("score_depth").AsDouble().NotNullable()
            .WithColumn("score_accuracy").AsDouble().NotNullable()
            .WithColumn("score_source_quality").AsDouble().NotNullable()
            .WithColumn("score_novelty").AsDouble().NotNullable()
            .WithColumn("score_coherence").AsDouble().NotNullable()
            .WithColumn("weighted_average").AsDouble().NotNullable()
            .WithColumn("strengths").AsString().Nullable()
            .WithColumn("improvements").AsString().Nullable()
            .WithColumn("critique").AsString().NotNullable()
            .WithColumn("endorses").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("tokens_used").AsInt32().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.Index("ix_aesir_research_peer_review_session_id")
            .OnTable("aesir_research_peer_review")
            .InSchema("aesir")
            .OnColumn("session_id");

        Create.Index("ix_aesir_research_peer_review_submission_id")
            .OnTable("aesir_research_peer_review")
            .InSchema("aesir")
            .OnColumn("submission_id");

        Create.ForeignKey("FK_aesir_research_peer_review_session_id_aesir_research_session_id")
            .FromTable("aesir_research_peer_review").InSchema("aesir").ForeignColumn("session_id")
            .ToTable("aesir_research_session").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_aesir_research_peer_review_submission_id_aesir_research_submission_id")
            .FromTable("aesir_research_peer_review").InSchema("aesir").ForeignColumn("submission_id")
            .ToTable("aesir_research_submission").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_aesir_research_peer_review_reviewer_agent_id_aesir_agent_id")
            .FromTable("aesir_research_peer_review").InSchema("aesir").ForeignColumn("reviewer_agent_id")
            .ToTable("aesir_agent").InSchema("aesir").PrimaryColumn("id");

        // 6. Create aesir_research_report table
        Create.Table("aesir_research_report")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("session_id").AsGuid().NotNullable()
            .WithColumn("title").AsString(500).NotNullable()
            .WithColumn("executive_summary").AsString().NotNullable()
            .WithColumn("methodology_section").AsString().NotNullable()
            .WithColumn("findings").AsCustom("jsonb").Nullable()
            .WithColumn("alternative_perspectives").AsString().Nullable()
            .WithColumn("research_gaps").AsString().Nullable()
            .WithColumn("bibliography").AsCustom("jsonb").Nullable()
            .WithColumn("full_markdown").AsString().NotNullable()
            .WithColumn("metadata").AsCustom("jsonb").Nullable()
            .WithColumn("tokens_used").AsInt32().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.Index("ix_aesir_research_report_session_id")
            .OnTable("aesir_research_report")
            .InSchema("aesir")
            .OnColumn("session_id")
            .Unique();

        Create.ForeignKey("FK_aesir_research_report_session_id_aesir_research_session_id")
            .FromTable("aesir_research_report").InSchema("aesir").ForeignColumn("session_id")
            .ToTable("aesir_research_session").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        // 7. Create aesir_research_trail table
        Create.Table("aesir_research_trail")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("session_id").AsGuid().NotNullable()
            .WithColumn("submission_id").AsGuid().Nullable()
            .WithColumn("event_type").AsInt16().NotNullable()
            .WithColumn("agent_role").AsInt16().Nullable()
            .WithColumn("description").AsString(1000).NotNullable()
            .WithColumn("input_json").AsCustom("jsonb").Nullable()
            .WithColumn("output_json").AsCustom("jsonb").Nullable()
            .WithColumn("duration_ms").AsInt64().Nullable()
            .WithColumn("timestamp").AsDateTimeOffset().NotNullable();

        Create.Index("ix_aesir_research_trail_session_id")
            .OnTable("aesir_research_trail")
            .InSchema("aesir")
            .OnColumn("session_id");

        Create.Index("ix_aesir_research_trail_timestamp")
            .OnTable("aesir_research_trail")
            .InSchema("aesir")
            .OnColumn("timestamp");

        Create.ForeignKey("FK_aesir_research_trail_session_id_aesir_research_session_id")
            .FromTable("aesir_research_trail").InSchema("aesir").ForeignColumn("session_id")
            .ToTable("aesir_research_session").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_aesir_research_trail_submission_id_aesir_research_submission_id")
            .FromTable("aesir_research_trail").InSchema("aesir").ForeignColumn("submission_id")
            .ToTable("aesir_research_submission").InSchema("aesir").PrimaryColumn("id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        // Drop tables in reverse order (respecting foreign key dependencies)
        Delete.Table("aesir_research_trail").InSchema("aesir");
        Delete.Table("aesir_research_report").InSchema("aesir");
        Delete.Table("aesir_research_peer_review").InSchema("aesir");
        Delete.Table("aesir_research_submission").InSchema("aesir");
        Delete.Table("aesir_research_session").InSchema("aesir");
        Delete.Table("aesir_research_team_member").InSchema("aesir");
        Delete.Table("aesir_research_team").InSchema("aesir");
    }
}

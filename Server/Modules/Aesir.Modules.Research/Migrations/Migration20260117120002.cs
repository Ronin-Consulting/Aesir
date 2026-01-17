using FluentMigrator;

namespace Aesir.Modules.Research.Migrations;

/// <summary>
/// Migration to remove unused aesir_research_trail table and related infrastructure.
/// The research trail feature was designed but never integrated into the research workflow.
/// </summary>
[Migration(20260117120002)]
public class Migration20260117120002 : Migration
{
    public override void Up()
    {
        // Drop the unused research trail table
        // Note: Foreign keys will be dropped automatically with CASCADE
        Delete.Table("aesir_research_trail").InSchema("aesir");
    }

    public override void Down()
    {
        // Recreate the research trail table if needed
        Create.Table("aesir_research_trail")
            .InSchema("aesir")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("session_id").AsGuid().NotNullable()
            .WithColumn("submission_id").AsGuid().Nullable()
            .WithColumn("event_type").AsInt32().NotNullable()
            .WithColumn("agent_role").AsInt32().Nullable()
            .WithColumn("description").AsString(1000).NotNullable()
            .WithColumn("input_json").AsCustom("jsonb").Nullable()
            .WithColumn("output_json").AsCustom("jsonb").Nullable()
            .WithColumn("duration_ms").AsInt64().Nullable()
            .WithColumn("timestamp").AsDateTimeOffset().NotNullable();

        Create.Index("ix_aesir_research_trail_session_id")
            .OnTable("aesir_research_trail").InSchema("aesir")
            .OnColumn("session_id");

        Create.Index("ix_aesir_research_trail_timestamp")
            .OnTable("aesir_research_trail").InSchema("aesir")
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
}

using FluentMigrator;

namespace Aesir.Modules.Research.Migrations;

[Migration(20251228120001, "Remove unused override columns from research team member")]
public class Migration20251228120001 : Migration
{
    public override void Up()
    {
        // Remove unused override columns - only override_temperature is kept
        Delete.Column("override_persona").FromTable("aesir_research_team_member").InSchema("aesir");
        Delete.Column("override_planning_prompt").FromTable("aesir_research_team_member").InSchema("aesir");
        Delete.Column("override_research_prompt").FromTable("aesir_research_team_member").InSchema("aesir");
        Delete.Column("override_thinking_mode").FromTable("aesir_research_team_member").InSchema("aesir");
        Delete.Column("override_tools").FromTable("aesir_research_team_member").InSchema("aesir");
    }

    public override void Down()
    {
        // Re-add the removed columns
        Alter.Table("aesir_research_team_member")
            .InSchema("aesir")
            .AddColumn("override_persona").AsString().Nullable()
            .AddColumn("override_planning_prompt").AsString().Nullable()
            .AddColumn("override_research_prompt").AsString().Nullable()
            .AddColumn("override_thinking_mode").AsInt16().Nullable()
            .AddColumn("override_tools").AsCustom("jsonb").Nullable();
    }
}

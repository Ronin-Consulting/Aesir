
using FluentMigrator;

namespace Aesir.Api.Server.Data.Migrations;

[Migration(20250924150901, "")]
public class Migration20250924150901 : Migration
{
    public override void Up()
    {
        Execute.Sql("""

                    insert into aesir.aesir_tool (name, type, description, tool_name)
                    values ('RAG', 0, 'Supports searching documents with Retrieval Augmented Generation', 'RagTool')
                    on conflict (name) do update set
                        type = excluded.type,
                        description = excluded.description,
                        tool_name = excluded.tool_name;
                    insert into aesir.aesir_tool(name, type, description, tool_name)
                    values ('Web', 0, 'Supports sending email through the tool''s configured account', 'WebTool')
                    on conflict (name) do update set
                        type = excluded.type, 
                        description = excluded.description,
                        tool_name = excluded.tool_name;

                    """);
    }

    public override void Down()
    {

    }
}
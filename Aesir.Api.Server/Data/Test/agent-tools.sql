insert into aesir.aesir_agent_tool (agent_id, tool_id)
values (
           (select id from aesir.aesir_agent where name = 'Agent 1'),
           (select id from aesir.aesir_tool where name = 'RAG')
       );
insert into aesir.aesir_agent_tool (agent_id, tool_id)
values (
           (select id from aesir.aesir_agent where name = 'Agent 2'),
           (select id from aesir.aesir_tool where name = 'RAG')
       );
insert into aesir.aesir_agent_tool (agent_id, tool_id)
values (
           (select id from aesir.aesir_agent where name = 'Computer Use'),
           (select id from aesir.aesir_tool where name = 'RAG')
       );


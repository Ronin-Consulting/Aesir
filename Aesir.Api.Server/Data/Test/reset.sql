delete from aesir_agent_tool;
delete from aesir_agent;
delete from aesir_mcp_server;
delete from aesir_tool where name not in ('Web', 'RAG');
update aesir_general_settings set rag_emb_inf_eng_id = null, rag_emb_model = null, rag_vis_inf_eng_id = null, rag_vis_model = null;
delete from aesir_inference_engine;

insert into aesir.aesir_agent (name, chat_model, embedding_model, vision_model, source, prompt)
values ('Agent 1', 'gpt-4.1-2025-04-14', 'text-embedding-3-large','gpt-4.1-2025-04-14', 0, 1);
insert into aesir.aesir_agent (name, chat_model, embedding_model, vision_model, source, prompt)
values ('Agent 2', 'qwen3:32b-q4_K_M', 'mxbai-embed-large:latest','gemma3:12b',1,1);
insert into aesir.aesir_agent (name, chat_model, embedding_model, vision_model, source, prompt)
values ('Computer Use', 'cogito:32b-v1-preview-qwen-q4_K_M', 'mxbai-embed-large:latest','gemma3:12b',1,0);
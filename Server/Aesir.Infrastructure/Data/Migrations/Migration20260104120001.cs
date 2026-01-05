using FluentMigrator;

namespace Aesir.Infrastructure.Data.Migrations;

/// <summary>
/// Converts JSONB columns from PascalCase to snake_case to match API serialization conventions.
/// This ensures consistency between database storage and JSON API contracts.
/// </summary>
[Migration(20260104120001, "Convert JSONB columns from PascalCase to snake_case")]
public class Migration20260104120001 : Migration
{
    public override void Up()
    {
        // Create a function to convert PascalCase to snake_case
        Execute.Sql(@"
            CREATE OR REPLACE FUNCTION aesir.pascal_to_snake_case(key text)
            RETURNS text AS $$
            BEGIN
                -- Insert underscore before uppercase letters (except first), then lowercase all
                RETURN lower(regexp_replace(key, '([a-z])([A-Z])', '\1_\2', 'g'));
            END;
            $$ LANGUAGE plpgsql IMMUTABLE;
        ");

        // Create a recursive function to convert all keys in a JSONB object
        Execute.Sql(@"
            CREATE OR REPLACE FUNCTION aesir.jsonb_keys_to_snake_case(data jsonb)
            RETURNS jsonb AS $$
            DECLARE
                result jsonb;
                key text;
                value jsonb;
                new_key text;
                arr_elem jsonb;
                new_arr jsonb[];
            BEGIN
                IF data IS NULL THEN
                    RETURN NULL;
                END IF;

                -- Handle objects
                IF jsonb_typeof(data) = 'object' THEN
                    result := '{}'::jsonb;
                    FOR key, value IN SELECT * FROM jsonb_each(data)
                    LOOP
                        new_key := aesir.pascal_to_snake_case(key);
                        result := result || jsonb_build_object(new_key, aesir.jsonb_keys_to_snake_case(value));
                    END LOOP;
                    RETURN result;

                -- Handle arrays
                ELSIF jsonb_typeof(data) = 'array' THEN
                    new_arr := ARRAY[]::jsonb[];
                    FOR arr_elem IN SELECT * FROM jsonb_array_elements(data)
                    LOOP
                        new_arr := array_append(new_arr, aesir.jsonb_keys_to_snake_case(arr_elem));
                    END LOOP;
                    RETURN to_jsonb(new_arr);

                -- Return primitives as-is
                ELSE
                    RETURN data;
                END IF;
            END;
            $$ LANGUAGE plpgsql IMMUTABLE;
        ");

        // Update aesir_research_report.metadata
        Execute.Sql(@"
            UPDATE aesir.aesir_research_report
            SET metadata = aesir.jsonb_keys_to_snake_case(metadata)
            WHERE metadata IS NOT NULL;
        ");

        // Update aesir_research_report.findings
        Execute.Sql(@"
            UPDATE aesir.aesir_research_report
            SET findings = aesir.jsonb_keys_to_snake_case(findings)
            WHERE findings IS NOT NULL;
        ");

        // Update aesir_research_report.bibliography
        Execute.Sql(@"
            UPDATE aesir.aesir_research_report
            SET bibliography = aesir.jsonb_keys_to_snake_case(bibliography)
            WHERE bibliography IS NOT NULL;
        ");

        // Update aesir_inference_engine.configuration
        Execute.Sql(@"
            UPDATE aesir.aesir_inference_engine
            SET configuration = aesir.jsonb_keys_to_snake_case(configuration)
            WHERE configuration IS NOT NULL;
        ");

        // Update aesir_research_submission.sources
        Execute.Sql(@"
            UPDATE aesir.aesir_research_submission
            SET sources = aesir.jsonb_keys_to_snake_case(sources)
            WHERE sources IS NOT NULL;
        ");

        // Update aesir_research_submission.tool_calls
        Execute.Sql(@"
            UPDATE aesir.aesir_research_submission
            SET tool_calls = aesir.jsonb_keys_to_snake_case(tool_calls)
            WHERE tool_calls IS NOT NULL;
        ");

        // Update aesir_research_trail.input_json (already jsonb, no cast needed)
        Execute.Sql(@"
            UPDATE aesir.aesir_research_trail
            SET input_json = aesir.jsonb_keys_to_snake_case(input_json)
            WHERE input_json IS NOT NULL;
        ");

        // Update aesir_research_trail.output_json (already jsonb, no cast needed)
        Execute.Sql(@"
            UPDATE aesir.aesir_research_trail
            SET output_json = aesir.jsonb_keys_to_snake_case(output_json)
            WHERE output_json IS NOT NULL;
        ");

        // Update aesir_log_inference.tool_calls
        Execute.Sql(@"
            UPDATE aesir.aesir_log_inference
            SET tool_calls = aesir.jsonb_keys_to_snake_case(tool_calls)
            WHERE tool_calls IS NOT NULL;
        ");

        // Update aesir_log_document.metadata
        Execute.Sql(@"
            UPDATE aesir.aesir_log_document
            SET metadata = aesir.jsonb_keys_to_snake_case(metadata)
            WHERE metadata IS NOT NULL;
        ");

        // Update aesir_chat_session.conversation (contains Messages, Role, Content)
        Execute.Sql(@"
            UPDATE aesir.aesir_chat_session
            SET conversation = aesir.jsonb_keys_to_snake_case(conversation)
            WHERE conversation IS NOT NULL;
        ");

        // Recreate full-text search index with snake_case JSON paths
        Execute.Sql("DROP INDEX IF EXISTS aesir.idx_aesir_chat_session_messages_text_search;");
        Execute.Sql(@"
            CREATE INDEX idx_aesir_chat_session_messages_text_search
            ON aesir.aesir_chat_session
            USING GIN (to_tsvector('english',
                COALESCE(jsonb_path_query_array(conversation, '$.messages[*] ? (@.role <> ""system"").content')::text, '')));
        ");

        // Keep the functions for future use (new data will be written in snake_case by the application)
    }

    public override void Down()
    {
        // Create a function to convert snake_case back to PascalCase
        Execute.Sql(@"
            CREATE OR REPLACE FUNCTION aesir.snake_to_pascal_case(key text)
            RETURNS text AS $$
            BEGIN
                -- Capitalize first letter and each letter after underscore, remove underscores
                RETURN regexp_replace(
                    initcap(replace(key, '_', ' ')),
                    ' ', '', 'g'
                );
            END;
            $$ LANGUAGE plpgsql IMMUTABLE;
        ");

        // Create a recursive function to convert all keys back to PascalCase
        Execute.Sql(@"
            CREATE OR REPLACE FUNCTION aesir.jsonb_keys_to_pascal_case(data jsonb)
            RETURNS jsonb AS $$
            DECLARE
                result jsonb;
                key text;
                value jsonb;
                new_key text;
                arr_elem jsonb;
                new_arr jsonb[];
            BEGIN
                IF data IS NULL THEN
                    RETURN NULL;
                END IF;

                IF jsonb_typeof(data) = 'object' THEN
                    result := '{}'::jsonb;
                    FOR key, value IN SELECT * FROM jsonb_each(data)
                    LOOP
                        new_key := aesir.snake_to_pascal_case(key);
                        result := result || jsonb_build_object(new_key, aesir.jsonb_keys_to_pascal_case(value));
                    END LOOP;
                    RETURN result;

                ELSIF jsonb_typeof(data) = 'array' THEN
                    new_arr := ARRAY[]::jsonb[];
                    FOR arr_elem IN SELECT * FROM jsonb_array_elements(data)
                    LOOP
                        new_arr := array_append(new_arr, aesir.jsonb_keys_to_pascal_case(arr_elem));
                    END LOOP;
                    RETURN to_jsonb(new_arr);

                ELSE
                    RETURN data;
                END IF;
            END;
            $$ LANGUAGE plpgsql IMMUTABLE;
        ");

        // Revert all JSONB columns back to PascalCase
        Execute.Sql(@"
            UPDATE aesir.aesir_research_report
            SET metadata = aesir.jsonb_keys_to_pascal_case(metadata)
            WHERE metadata IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_research_report
            SET findings = aesir.jsonb_keys_to_pascal_case(findings)
            WHERE findings IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_research_report
            SET bibliography = aesir.jsonb_keys_to_pascal_case(bibliography)
            WHERE bibliography IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_inference_engine
            SET configuration = aesir.jsonb_keys_to_pascal_case(configuration)
            WHERE configuration IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_research_submission
            SET sources = aesir.jsonb_keys_to_pascal_case(sources)
            WHERE sources IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_research_submission
            SET tool_calls = aesir.jsonb_keys_to_pascal_case(tool_calls)
            WHERE tool_calls IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_research_trail
            SET input_json = aesir.jsonb_keys_to_pascal_case(input_json)
            WHERE input_json IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_research_trail
            SET output_json = aesir.jsonb_keys_to_pascal_case(output_json)
            WHERE output_json IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_log_inference
            SET tool_calls = aesir.jsonb_keys_to_pascal_case(tool_calls)
            WHERE tool_calls IS NOT NULL;
        ");

        Execute.Sql(@"
            UPDATE aesir.aesir_log_document
            SET metadata = aesir.jsonb_keys_to_pascal_case(metadata)
            WHERE metadata IS NOT NULL;
        ");

        // Revert aesir_chat_session.conversation
        Execute.Sql(@"
            UPDATE aesir.aesir_chat_session
            SET conversation = aesir.jsonb_keys_to_pascal_case(conversation)
            WHERE conversation IS NOT NULL;
        ");

        // Recreate full-text search index with PascalCase JSON paths
        Execute.Sql("DROP INDEX IF EXISTS aesir.idx_aesir_chat_session_messages_text_search;");
        Execute.Sql(@"
            CREATE INDEX idx_aesir_chat_session_messages_text_search
            ON aesir.aesir_chat_session
            USING GIN (to_tsvector('english',
                COALESCE(jsonb_path_query_array(conversation, '$.Messages[*] ? (@.Role <> ""system"").Content')::text, '')));
        ");

        // Clean up functions
        Execute.Sql("DROP FUNCTION IF EXISTS aesir.jsonb_keys_to_pascal_case(jsonb);");
        Execute.Sql("DROP FUNCTION IF EXISTS aesir.snake_to_pascal_case(text);");
        Execute.Sql("DROP FUNCTION IF EXISTS aesir.jsonb_keys_to_snake_case(jsonb);");
        Execute.Sql("DROP FUNCTION IF EXISTS aesir.pascal_to_snake_case(text);");
    }
}

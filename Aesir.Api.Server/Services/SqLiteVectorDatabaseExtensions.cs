using System.Reflection;
using System.Text.Json;
using LangChain.Databases;
using LangChain.Databases.Sqlite;
using Microsoft.Data.Sqlite;

namespace Aesir.Api.Server.Services;

public static class SqLiteVectorDatabaseExtensions
{
    public static async Task<IEnumerable<DocumentInfo>> GetVectorCollectionDocumentEmbeddingsAsync(
        this SqLiteVectorDatabase vectorDatabase, string collectionName)
    {
        var exists = await vectorDatabase.IsCollectionExistsAsync(collectionName);

        if (exists)
        {
            var type = vectorDatabase.GetType();
            
            // not optimal, but it works
            var privateField = type.GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if(privateField == null)
            {
                throw new InvalidOperationException("Field '_connection' not found.");
            }
            
            var connection = (SqliteConnection)privateField.GetValue(vectorDatabase)!;
            
            var searchCommand = connection.CreateCommand();
            string query = $"SELECT id, vector, document FROM {collectionName} ORDER BY id";
            searchCommand.CommandText = query;
            var result = new List<DocumentEmbeddingInfo>();
            var reader = await searchCommand.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var id = reader.GetString(0);
                var vec = await reader.GetFieldValueAsync<string>(1).ConfigureAwait(false);
                var doc = await reader.GetFieldValueAsync<string>(2).ConfigureAwait(false);
                
                var vecDeserialized = JsonSerializer.Deserialize<float[]>(vec) ?? [];
                var docDeserialized = JsonSerializer.Deserialize<Vector>(doc) ?? new Vector
                {
                    Text = string.Empty,
                };
                
                var path = docDeserialized.Metadata?["path"];
                var documentFilename = Path.GetFileName(path?.ToString() ?? "nofilename");
                
                result.Add(new DocumentEmbeddingInfo(id, documentFilename, vecDeserialized, docDeserialized.Text));
            }

            // group the results by document name
            return result.GroupBy(x => x.Name)
                .Select(x => new DocumentInfo(x.Key, x.ToList()))
                .ToList();
        }
        else
        {
            return Array.Empty<DocumentInfo>();
        }
    }
}

public record DocumentInfo(string Name, IEnumerable<DocumentEmbeddingInfo> DocumentEmbeddings);
public record DocumentEmbeddingInfo(string Id, string Name, float[] Emending, string TextChunk);
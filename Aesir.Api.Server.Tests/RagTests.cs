using Aesir.Api.Server.Services;
using LangChain.Databases.Sqlite;
using LangChain.DocumentLoaders;
using LangChain.Extensions;
using LangChain.Providers;
using LangChain.Providers.Ollama;
using LangChain.Splitters.Text;

namespace Aesir.Api.Server.Tests;

[TestClass]
public class RagTests
{
    [TestMethod]
    public async Task SimpleTest()
    {
        const string embeddingModelId = "nomic-embed-text";
        const string chatModelId = "llama3.1:8b-instruct-q5_K_M";
        const string vectorCollectionName = "harrypotter";
        
        var provider = new OllamaProvider();
        var embeddingModel = new OllamaEmbeddingModel(provider, id: embeddingModelId);
        var llm = new OllamaChatModel(provider, id: chatModelId);

        var vectorDatabase = new SqLiteVectorDatabase(dataSource: "vectors.db");

        await vectorDatabase.DeleteCollectionAsync(vectorCollectionName);
        
        var documentLoaderSettings = new DocumentLoaderSettings
        {
            ShouldCollectMetadata = true,
        };

        var dataSource = DataSource.FromUrl(
            "https://canonburyprimaryschool.co.uk/wp-content/uploads/2016/01/Joanne-K.-Rowling-Harry-Potter-Book-1-Harry-Potter-and-the-Philosophers-Stone-EnglishOnlineClub.com_.pdf");
        var documentLoader = new PdfPigPdfLoader();
        
        var chunkCalculator = new DocumentChunkCalculator(documentLoader, dataSource);

        var chunkSize = await chunkCalculator.CalculateChunkSizeAsync();
        var chunkOverlap = await chunkCalculator.CalculateChunkOverlapAsync(chunkSize);

        var textSplitter = new CharacterTextSplitter(
            chunkSize: chunkSize,
            chunkOverlap: chunkOverlap);
        
        var vectorCollection = await vectorDatabase.AddDocumentsFromAsync<PdfPigPdfLoader>(
            embeddingModel,
            dimensions: 768, // check the embedding model for the correct dimensions
            dataSource: DataSource.FromUrl(
                "https://canonburyprimaryschool.co.uk/wp-content/uploads/2016/01/Joanne-K.-Rowling-Harry-Potter-Book-1-Harry-Potter-and-the-Philosophers-Stone-EnglishOnlineClub.com_.pdf"),
            collectionName: vectorCollectionName,
            textSplitter: textSplitter,
            loaderSettings: documentLoaderSettings,
            behavior: AddDocumentsToDatabaseBehavior.JustReturnCollectionIfCollectionIsAlreadyExists);

        const string question = "What is Harry's Address?";//"What was Harry Potter's address?";//"What is Harry's Address?";
        var similarDocuments = await vectorCollection.GetSimilarDocuments(embeddingModel, question, amount: 5);
        // Use similar documents and LLM to answer the question
        var answer = await llm.GenerateAsync(
            $"""
             Use the following pieces of context to answer the question at the end.
             If the answer is not in context then just say that you don't know, don't try to make up an answer.
             Keep the answer as short as possible.

             {similarDocuments.AsString()}

             Question: {question}
             Helpful Answer:
             """);

        Console.WriteLine($"LLM answer: {answer}");
    }
    
    [TestMethod]
    public async Task ChatCondensedTest()
    {
        const string embeddingModelId = "nomic-embed-text";
        const string chatModelId = "llama3.1:8b-instruct-q5_K_M";
        const string vectorCollectionName = "harrypotter";
        
        var provider = new OllamaProvider();
        var embeddingModel = new OllamaEmbeddingModel(provider, id: embeddingModelId);
        var llm = new OllamaChatModel(provider, id: chatModelId);

        var vectorDatabase = new SqLiteVectorDatabase(dataSource: "vectors.db");

        await vectorDatabase.DeleteCollectionAsync(vectorCollectionName);
        
        var documentLoaderSettings = new DocumentLoaderSettings
        {
            ShouldCollectMetadata = true,
        };
        
        var dataSource = DataSource.FromUrl(
            "https://canonburyprimaryschool.co.uk/wp-content/uploads/2016/01/Joanne-K.-Rowling-Harry-Potter-Book-1-Harry-Potter-and-the-Philosophers-Stone-EnglishOnlineClub.com_.pdf");
        var documentLoader = new PdfPigPdfLoader();
        
        var chunkCalculator = new DocumentChunkCalculator(documentLoader, dataSource);

        var chunkSize = await chunkCalculator.CalculateChunkSizeAsync();
        var chunkOverlap = await chunkCalculator.CalculateChunkOverlapAsync(chunkSize);

        var textSplitter = new CharacterTextSplitter(
            chunkSize: chunkSize,
            chunkOverlap: chunkOverlap);
        
        var vectorCollection = await vectorDatabase.AddDocumentsFromAsync<PdfPigPdfLoader>(
            embeddingModel,
            dimensions: 768, // check the embedding model for the correct dimensions
            dataSource: DataSource.FromUrl(
                "https://canonburyprimaryschool.co.uk/wp-content/uploads/2016/01/Joanne-K.-Rowling-Harry-Potter-Book-1-Harry-Potter-and-the-Philosophers-Stone-EnglishOnlineClub.com_.pdf"),
            collectionName: vectorCollectionName,
            textSplitter: textSplitter,
            loaderSettings: documentLoaderSettings,
            behavior: AddDocumentsToDatabaseBehavior.JustReturnCollectionIfCollectionIsAlreadyExists);
        
        // simulate conversation
        var messages = new List<Message>
        {
            PromptLibrary.DefaultSystemPromptTemplate.Replace("{current_datetime}",DateTime.Now.ToString("f")).AsSystemMessage(),
            "What were Harry Potter's parents name?".AsHumanMessage(),
            "Harry Potter’s parents were Lily and James Potter.".AsAiMessage()
        };

        // now we want to condense the conversation
        var messagesAsString = string.Join(Environment.NewLine,messages.Select(x => x.ToString()));

        var condensedPrompt = PromptLibrary.DefaultCondensePromptTemplate
            .Replace("{chat_history}", messagesAsString)
            .Replace("{question}", "What is Harry's Address?");
        
        // now ask llm to condense the conversation
        var result = await llm.GenerateAsync(condensedPrompt);
        
        var standAloneQuestion = result.ToString();
        
        var similarDocuments = await vectorCollection.GetSimilarDocuments(embeddingModel, standAloneQuestion, amount: 5);
        
        var similarDocumentsAsString = similarDocuments.AsString();
        
        // Use similar documents and LLM to answer the question
        var answer = await llm.GenerateAsync(
            $"""
             Use the following pieces of context to answer the question at the end.
             If the answer is not in context then just say that you don't know, don't try to make up an answer.
             Keep the answer as short as possible.

             {similarDocumentsAsString}

             Question: {standAloneQuestion}
             Helpful Answer:
             """);
        
        
        Console.WriteLine($"LLM answer: {answer}");
    }
    
    [TestMethod]
    public async Task VectorStoreTest()
    {
        const string embeddingModelId = "nomic-embed-text";
        const string vectorCollectionName = "foobar";
        
        var provider = new OllamaProvider();
        var embeddingModel = new OllamaEmbeddingModel(provider, id: embeddingModelId);

        var vectorDatabase = new SqLiteVectorDatabase(dataSource: "vectors.db");

        await vectorDatabase.DeleteCollectionAsync(vectorCollectionName);
        
        var documentLoaderSettings = new DocumentLoaderSettings
        {
            ShouldCollectMetadata = true,
        };
        
        await vectorDatabase.AddDocumentsFromAsync<PdfPigPdfLoader>(
            embeddingModel,
            dimensions: 768, // check the embedding model for the correct dimensions
            dataSource: DataSource.FromUrl(
                "https://canonburyprimaryschool.co.uk/wp-content/uploads/2016/01/Joanne-K.-Rowling-Harry-Potter-Book-1-Harry-Potter-and-the-Philosophers-Stone-EnglishOnlineClub.com_.pdf"),
            collectionName: vectorCollectionName,
            loaderSettings: documentLoaderSettings,
            behavior: AddDocumentsToDatabaseBehavior.JustReturnCollectionIfCollectionIsAlreadyExists);

        var results = await vectorDatabase.GetVectorCollectionDocumentEmbeddingsAsync(vectorCollectionName);
        
        foreach (var result in results)
        {
            Console.WriteLine($"Document: {result.Name}");
            foreach (var embedding in result.DocumentEmbeddings)
            {
                Console.WriteLine($"Embedding: {embedding.Id}");
            }
        }
    }

    [TestMethod]
    public async Task ChunkSizeCalculatorText()
    {
        var dataSource = DataSource.FromUrl(
            "https://canonburyprimaryschool.co.uk/wp-content/uploads/2016/01/Joanne-K.-Rowling-Harry-Potter-Book-1-Harry-Potter-and-the-Philosophers-Stone-EnglishOnlineClub.com_.pdf");

        var chunkSize = await GetChunkSizeAsync(dataSource);
        var chunkOverlap = await GetChunkOverlapAsync(dataSource, chunkSize);
        
        Console.WriteLine($"Chunk size: {chunkSize}");
        Console.WriteLine($"Chunk overlap: {chunkOverlap}");
    }

    private static async Task<int> GetChunkOverlapAsync(DataSource dataSource, int chunkSize)
    {
        var documentLoader = new PdfPigPdfLoader();
        
        var calculator = new DocumentChunkCalculator(documentLoader, dataSource);
        
        return await calculator.CalculateChunkOverlapAsync(chunkSize);
    }
    
    private static async Task<int> GetChunkSizeAsync(DataSource dataSource)
    {
        var documentLoader = new PdfPigPdfLoader();

        var calculator = new DocumentChunkCalculator(documentLoader, dataSource);
        
        return await calculator.CalculateChunkSizeAsync();        
    }
}
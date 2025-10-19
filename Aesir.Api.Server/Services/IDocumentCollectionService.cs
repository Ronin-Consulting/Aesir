using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides document collection functionality for managing document storage and retrieval with kernel plugin integration.
/// </summary>
public interface IDocumentCollectionService
{
    /// <summary>
    /// Loads a document from the specified path into the collection.
    /// </summary>
    /// <param name="documentPath">The path to the document to load.</param>
    /// <param name="modelLocationDescriptor">Location of the model and associated inference engine to use for loading
    /// information from the document.</param>
    /// <param name="fileMetaData">Optional metadata to associate with the document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadDocumentAsync(string documentPath, ModelLocationDescriptor modelLocationDescriptor,
        IDictionary<string, object>? fileMetaData = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a document from the collection based on metadata.
    /// </summary>
    /// <param name="fileMetaData">The metadata used to identify the document to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns true if the document was deleted successfully.</returns>
    Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes multiple documents from the collection based on arguments.
    /// </summary>
    /// <param name="args">The arguments used to identify the documents to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteDocumentsAsync(IDictionary<string, object>? args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets kernel plugin functions for document operations.
    /// </summary>
    /// <param name="kernelPluginArguments">Optional arguments for the kernel plugin.</param>
    /// <returns>A task representing the asynchronous operation that returns a list of KernelFunctions needed for the plugin for operations.</returns>
    Task<IList<KernelFunction>> GetKernelPluginFunctionsAsync(IDictionary<string, object>? kernelPluginArguments = null);
}
namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides arguments for global document collection operations.
/// </summary>
public class GlobalDocumentCollectionArgs : Dictionary<string, object>
{
    /// <summary>
    /// Gets the default instance of global document collection arguments.
    /// </summary>
    public static GlobalDocumentCollectionArgs Default => new();
    
    /// <summary>
    /// Gets the document collection type.
    /// </summary>
    public DocumentCollectionType DocumentCollectionType => (DocumentCollectionType) this["DocumentCollectionType"];

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalDocumentCollectionArgs"/> class.
    /// </summary>
    private GlobalDocumentCollectionArgs()
    {
        this["DocumentCollectionType"] = DocumentCollectionType.Global;
    }

    /// <summary>
    /// Sets the category identifier for the document collection.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    public void SetCategoryId(string categoryId)
    {
        this["CategoryId"] = categoryId;   
    }
}
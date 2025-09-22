using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Represents information about an AI model including its capabilities and metadata.
/// </summary>
public class AesirModelInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the organization or entity that owns the model.
    /// </summary>
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the model was created.
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports chat completions.
    /// </summary>
    [JsonPropertyName("is_chat_model")]
    public bool IsChatModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports text embeddings.
    /// </summary>
    [JsonPropertyName("is_embedding_model")]
    public bool IsEmbeddingModel { get; set; }

    /// <summary>
    /// Gets or sets additional details about the AI model, such as its parent model,
    /// format, family, and other descriptive metadata.
    /// </summary>
    [JsonPropertyName("details")]
    public AesirModelDetails? Details { get; set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// In this implementation, it returns the value of the Id property.
    /// </summary>
    /// <returns>A string that represents the Id of the model.</returns>
    public override string ToString() => Id;

    /// <summary>
    /// Determines whether the specified <see cref="AesirModelInfo"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="AesirModelInfo"/> instance to compare with the current object.</param>
    /// <returns>True if the specified <see cref="AesirModelInfo"/> is equal to the current object, otherwise false.</returns>
    protected bool Equals(AesirModelInfo other)
    {
        return Id == other.Id;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((AesirModelInfo)obj);
    }

    /// <summary>
    /// Returns a hash code for the current object.
    /// The hash code is derived from the Id property of the model.
    /// </summary>
    /// <returns>An integer representing the hash code of the model based on its Id.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

/// <summary>
/// Represents detailed metadata about an AI model, including information about its parent model,
/// format, family, parameter size, and quantization level.
/// </summary>
public class AesirModelDetails
{
    /// <summary>
    /// Gets or sets the name of the parent model on which the model is based.
    /// </summary>
    [JsonPropertyName("parent_model")]
    public string? ParentModel { get; set; }

    /// <summary>Gets or sets the format of the model file.</summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>Gets or sets the family of the model.</summary>
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    /// <summary>Gets or sets the families of the model.</summary>
    [JsonPropertyName("families")]
    public string[]? Families { get; set; }

    /// <summary>Gets or sets the number of parameters in the model.</summary>
    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; set; }

    /// <summary>Gets or sets the quantization level of the model.</summary>
    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }
    
    [JsonPropertyName("capabilities")]
    public string[]? Capabilities { get; set; }
    
    [JsonExtensionData]
    public IDictionary<string, object>? ExtraInfo { get; set; }
    
    public override string ToString()
    {
        var details = new List<string>();
        if (!string.IsNullOrEmpty(ParentModel)) details.Add($"Parent Model: \n\t{ParentModel}");
        if (!string.IsNullOrEmpty(Format)) details.Add($"Format: \n\t{Format}");
        if (!string.IsNullOrEmpty(Family)) details.Add($"Family: \n\t{Family}");
        if (Families is { Length: > 0 }) details.Add($"Families: \n\t{string.Join(", ", Families)}");
        if (!string.IsNullOrEmpty(ParameterSize)) details.Add($"Parameter Size: \n\t{ParameterSize}");
        if (!string.IsNullOrEmpty(QuantizationLevel)) details.Add($"Quantization Level: \n\t{QuantizationLevel}");
        if(Capabilities is { Length: > 0 }) details.Add($"Capabilities: \n\t{string.Join(",\n\t", Capabilities)}");
        if(ExtraInfo is { Count: > 0 }) details.Add($"Extra Info: \n\t{string.Join(",\n\t", ExtraInfo.Select(x => $"{x.Key}: {x.Value}"))}");
        if(!string.IsNullOrEmpty(License)) details.Add($"License: {License}");
        
        return string.Join("\n", details);
    }
}
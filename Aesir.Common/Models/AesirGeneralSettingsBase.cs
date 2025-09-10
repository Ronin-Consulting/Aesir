using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirGeneralSettingsBase
{   
    /// <summary>
    /// Gets or sets the Inference engine id for the source of the RAG embedding model general setting
    /// </summary>
    [JsonPropertyName("rag_emb_inf_eng_id")]
    public Guid? RagEmbeddingInferenceEngineId { get; set; }
    
    /// <summary>
    /// Gets or sets the RAG embedding model id general setting
    /// </summary>
    [JsonPropertyName("rag_emb_model")]
    public string? RagEmbeddingModel { get; set; }
    
    /// <summary>
    /// Gets or sets the Inference engine id for the source of the RAG vision model general setting
    /// </summary>
    [JsonPropertyName("rag_vis_inf_eng_id")]
    public Guid? RagVisionInferenceEngineId { get; set; }
    
    /// <summary>
    /// Gets or sets the RAG vision model id general setting
    /// </summary>
    [JsonPropertyName("rag_vis_model")]
    public string? RagVisionModel { get; set; }
    
    /// <summary>
    /// Gets or sets the Text to Speech Model general setting
    /// </summary>
    [JsonPropertyName("tts_model_path")]
    public string? TtsModelPath { get; set; }
    
    /// <summary>
    /// Gets or sets the Speech to Text Model general setting
    /// </summary>
    [JsonPropertyName("stt_model_path")]
    public string? SttModelPath { get; set; }
    
    /// <summary>
    /// Gets or sets the Voice Activity Detection Model general setting
    /// </summary>
    [JsonPropertyName("vad_model_path")]
    public string? VadModelPath { get; set; }
}
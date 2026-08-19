namespace LzDetailingStore.Infrastructure.AI;

public sealed class EmbeddingOptions
{
    public string ModelPath { get; set; } = "models/all-MiniLM-L6-v2.onnx";
    public string TokenizerPath { get; set; } = "models/tokenizer.json";
    public int MaxTokens { get; set; } = 256;
    public int EmbeddingDimension { get; set; } = 384;
}

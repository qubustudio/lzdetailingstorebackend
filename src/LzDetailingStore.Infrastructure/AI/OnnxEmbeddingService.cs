using LzDetailingStore.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace LzDetailingStore.Infrastructure.AI;

public sealed class OnnxEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession? _session;
    private readonly ILogger<OnnxEmbeddingService> _logger;
    private readonly int _embeddingDimension;

    public bool IsAvailable => _session is not null;

    public OnnxEmbeddingService(IOptions<EmbeddingOptions> options, ILogger<OnnxEmbeddingService> logger)
    {
        _logger = logger;
        _embeddingDimension = options.Value.EmbeddingDimension;

        var modelPath = options.Value.ModelPath;

        if (!File.Exists(modelPath))
        {
            _logger.LogWarning("ONNX model not found at {Path}. Embeddings will be unavailable.", modelPath);
            _session = null;
            return;
        }

        try
        {
            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            _session = new InferenceSession(modelPath, sessionOptions);
            _logger.LogInformation("ONNX Embedding model loaded from {Path}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ONNX model from {Path}", modelPath);
            _session = null;
        }
    }

    public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (_session is null || string.IsNullOrWhiteSpace(text))
            return Task.FromResult<float[]?>(null);

        try
        {
            var tokens = TokenizeSimple(text);

            var inputIds = new DenseTensor<long>(tokens.Select(t => (long)t).ToArray(), [1, tokens.Length]);
            var attentionMask = new DenseTensor<long>(Enumerable.Repeat(1L, tokens.Length).ToArray(), [1, tokens.Length]);
            var tokenTypeIds = new DenseTensor<long>(new long[tokens.Length], [1, tokens.Length]);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
            };

            using var results = _session.Run(inputs);

            var output = results.First().AsTensor<float>();
            var embedding = MeanPool(output, tokens.Length);

            // L2 normalize
            var norm = MathF.Sqrt(embedding.Sum(x => x * x));
            if (norm > 0)
            {
                for (var i = 0; i < embedding.Length; i++)
                    embedding[i] /= norm;
            }

            return Task.FromResult<float[]?>(embedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text");
            return Task.FromResult<float[]?>(null);
        }
    }

    private int[] TokenizeSimple(string text)
    {
        // Simplified tokenization — production would use BPE tokenizer
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokens = new List<int> { 101 }; // [CLS]

        foreach (var word in words.Take(254))
        {
            tokens.Add(Math.Abs(word.GetHashCode()) % 30000 + 1000);
        }

        tokens.Add(102); // [SEP]
        return tokens.ToArray();
    }

    private float[] MeanPool(Tensor<float> output, int seqLength)
    {
        var embedding = new float[_embeddingDimension];
        var dimensions = output.Dimensions.ToArray();
        var hiddenSize = dimensions.Length > 2 ? dimensions[2] : _embeddingDimension;
        var actualDim = Math.Min(hiddenSize, _embeddingDimension);

        for (var i = 0; i < seqLength; i++)
        {
            for (var j = 0; j < actualDim; j++)
            {
                embedding[j] += output[0, i, j];
            }
        }

        for (var j = 0; j < actualDim; j++)
            embedding[j] /= seqLength;

        return embedding;
    }

    public void Dispose() => _session?.Dispose();
}

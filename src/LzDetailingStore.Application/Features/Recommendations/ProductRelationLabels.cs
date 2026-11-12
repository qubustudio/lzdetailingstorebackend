using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Application.Features.Recommendations;

/// <summary>
/// Maps product-relation enums to/from the Spanish values used by the public API contract.
/// </summary>
public static class ProductRelationLabels
{
    public const string Complementary = "complementario";
    public const string Similar = "similar";

    public const string Draft = "borrador";
    public const string Approved = "aprobado";
    public const string Rejected = "rechazado";

    public const string Ai = "ia";
    public const string Manual = "manual";

    public const string ModeNew = "nuevos";
    public const string ModeFill = "completar";

    public static readonly string[] TypeValues = [Complementary, Similar];
    public static readonly string[] StatusValues = [Draft, Approved, Rejected];
    public static readonly string[] ModeValues = [ModeNew, ModeFill];

    public static string ToLabel(this ProductRelationType type) => type switch
    {
        ProductRelationType.Complementary => Complementary,
        ProductRelationType.Similar => Similar,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string ToLabel(this ProductRelationStatus status) => status switch
    {
        ProductRelationStatus.Draft => Draft,
        ProductRelationStatus.Approved => Approved,
        ProductRelationStatus.Rejected => Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static string ToLabel(this ProductRelationSource source) => source switch
    {
        ProductRelationSource.Ai => Ai,
        ProductRelationSource.Manual => Manual,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static bool TryParseType(string? value, out ProductRelationType type)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case Complementary: type = ProductRelationType.Complementary; return true;
            case Similar: type = ProductRelationType.Similar; return true;
            default: type = default; return false;
        }
    }

    public static bool TryParseStatus(string? value, out ProductRelationStatus status)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case Draft: status = ProductRelationStatus.Draft; return true;
            case Approved: status = ProductRelationStatus.Approved; return true;
            case Rejected: status = ProductRelationStatus.Rejected; return true;
            default: status = default; return false;
        }
    }

    public static bool TryParseMode(string? value, out ProductRelationGenerationMode mode)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case ModeNew: mode = ProductRelationGenerationMode.New; return true;
            case ModeFill: mode = ProductRelationGenerationMode.Fill; return true;
            default: mode = default; return false;
        }
    }
}

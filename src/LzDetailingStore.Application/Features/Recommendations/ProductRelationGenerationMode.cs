namespace LzDetailingStore.Application.Features.Recommendations;

public enum ProductRelationGenerationMode
{
    /// <summary>Products with no relations at all (any status), plus the reverse direction.</summary>
    New,

    /// <summary>Products with fewer than 2 visible approved relations for a type.</summary>
    Fill
}

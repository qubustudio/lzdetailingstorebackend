using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Catalog.Queries.GetBrands;
using LzDetailingStore.Application.Features.Catalog.Queries.GetCategories;
using LzDetailingStore.Application.Features.Catalog.Queries.GetProductBySlug;
using LzDetailingStore.Application.Features.Catalog.Queries.GetProducts;
using LzDetailingStore.Application.Features.Intelligence.Queries.GetRecommendations;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Catalog;

public sealed class CatalogEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/v1/productos").WithTags("Products");
        products.MapGet("/", GetProducts);
        products.MapGet("/{slug}", GetProductBySlug);
        products.MapGet("/{id:guid}/recomendaciones", GetRecommendations);

        var brands = app.MapGroup("/api/v1/marcas").WithTags("Brands");
        brands.MapGet("/", GetBrands);

        var categories = app.MapGroup("/api/v1/categorias").WithTags("Categories");
        categories.MapGet("/", GetCategories);
    }

    private static async Task<IResult> GetProducts(
        ISender sender, CancellationToken ct,
        int page = 1, int limit = 20,
        Guid? brandId = null, Guid? categoryId = null,
        decimal? minPrice = null, decimal? maxPrice = null,
        string? search = null, string? sortBy = null)
    {
        var query = new GetProductsQuery(page, limit, brandId, categoryId, minPrice, maxPrice, search, sortBy);
        var result = await sender.Send(query, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetProductBySlug(
        string slug, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetProductBySlugQuery(slug), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetBrands(
        ISender sender, CancellationToken ct, bool? destacada = null)
    {
        var result = await sender.Send(new GetBrandsQuery(destacada), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetCategories(
        ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCategoriesQuery(), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetRecommendations(
        Guid id, ISender sender, CancellationToken ct, int count = 6)
    {
        var result = await sender.Send(new GetRecommendationsQuery(id, count), ct);
        return result.ToApiResponse();
    }
}

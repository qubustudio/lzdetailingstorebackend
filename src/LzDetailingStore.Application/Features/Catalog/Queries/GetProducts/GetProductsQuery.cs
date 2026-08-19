using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int Limit = 20,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Search = null,
    string? SortBy = null) : IRequest<Result<PagedResult<ProductListDto>>>;

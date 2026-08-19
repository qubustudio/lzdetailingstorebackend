using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDto>>;

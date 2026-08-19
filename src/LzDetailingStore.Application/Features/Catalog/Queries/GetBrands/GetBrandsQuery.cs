using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetBrands;

public sealed record GetBrandsQuery(bool? Destacada = null) : IRequest<Result<List<BrandDto>>>;

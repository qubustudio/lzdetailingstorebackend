using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Intelligence.Queries.GetRecommendations;

public sealed record GetRecommendationsQuery(Guid ProductId, int Count = 6) : IRequest<Result<List<ProductListDto>>>;

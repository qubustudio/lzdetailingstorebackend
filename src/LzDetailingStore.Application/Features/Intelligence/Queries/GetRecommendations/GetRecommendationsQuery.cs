using LzDetailingStore.Application.Features.Intelligence.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Intelligence.Queries.GetRecommendations;

public sealed record GetRecommendationsQuery(Guid ProductId, int Count = 6) : IRequest<Result<ProductRecommendationsDto>>;

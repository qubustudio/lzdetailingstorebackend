using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetCart;

public sealed record GetCartQuery(Guid UserId) : IRequest<Result<CartDto>>;

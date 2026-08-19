using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAddresses;

public sealed record GetAddressesQuery(Guid UserId) : IRequest<Result<List<ShippingAddressDto>>>;

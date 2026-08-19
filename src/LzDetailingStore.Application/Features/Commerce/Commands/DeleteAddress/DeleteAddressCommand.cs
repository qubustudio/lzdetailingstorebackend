using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.DeleteAddress;

public sealed record DeleteAddressCommand(Guid Id, Guid UserId) : IRequest<Result>;

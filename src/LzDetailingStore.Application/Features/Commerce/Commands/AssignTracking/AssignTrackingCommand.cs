using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.AssignTracking;

public sealed record AssignTrackingCommand(
    Guid OrderId,
    string TrackingNumber,
    string? TrackingUrl) : IRequest<Result>;

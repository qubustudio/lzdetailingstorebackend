using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.ProcessWebhook;

public sealed record ProcessWebhookCommand(
    string XSignature,
    string XRequestId,
    string DataId,
    string Type) : IRequest<Result>;

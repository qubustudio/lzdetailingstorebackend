using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.DeleteProductRelation;

public sealed record DeleteProductRelationCommand(Guid Id) : IRequest<Result>;

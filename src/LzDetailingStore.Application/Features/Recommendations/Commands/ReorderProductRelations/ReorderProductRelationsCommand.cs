using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.ReorderProductRelations;

/// <param name="Tipo">"complementario" or "similar".</param>
/// <param name="Ids">Relation ids of that product and type, in the desired order.</param>
public sealed record ReorderProductRelationsCommand(
    Guid ProductId,
    string Tipo,
    IReadOnlyList<Guid> Ids) : IRequest<Result>;

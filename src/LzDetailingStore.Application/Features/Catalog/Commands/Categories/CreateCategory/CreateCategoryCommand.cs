using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Nombre,
    string? Descripcion,
    int Orden) : IRequest<Result<Guid>>;

using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Nombre,
    string? Descripcion,
    int Orden) : IRequest<Result>;

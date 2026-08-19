using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;

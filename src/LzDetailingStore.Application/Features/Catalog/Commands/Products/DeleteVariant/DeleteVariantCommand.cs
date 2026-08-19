using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteVariant;

public sealed record DeleteVariantCommand(Guid Id) : IRequest<Result>;

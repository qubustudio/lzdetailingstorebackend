using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;

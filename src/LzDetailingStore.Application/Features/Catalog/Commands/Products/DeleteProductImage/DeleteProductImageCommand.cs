using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProductImage;

public sealed record DeleteProductImageCommand(Guid Id) : IRequest<Result>;

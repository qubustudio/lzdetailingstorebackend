using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSpecs;

public sealed record ProductSpecItem(string Label, string Value, int Orden);

public sealed record ManageProductSpecsCommand(
    Guid ProductId,
    IList<ProductSpecItem> Specs) : IRequest<Result>;

using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Brands.DeleteBrand;

public sealed record DeleteBrandCommand(Guid Id) : IRequest<Result>;

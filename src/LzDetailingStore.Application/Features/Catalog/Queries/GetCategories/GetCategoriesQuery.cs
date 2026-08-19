using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<Result<List<CategoryDto>>>;

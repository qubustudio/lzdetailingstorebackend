using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProducts;

public sealed class GetAdminProductsQueryValidator : AbstractValidator<GetAdminProductsQuery>
{
    public GetAdminProductsQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(GetAdminProductsQuery.SearchMaxLength)
            .WithMessage($"La búsqueda no puede exceder {GetAdminProductsQuery.SearchMaxLength} caracteres.");
    }
}

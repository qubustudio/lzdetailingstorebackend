using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProducts;

public sealed class GetAdminProductsQueryValidator : AbstractValidator<GetAdminProductsQuery>
{
    public GetAdminProductsQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(GetAdminProductsQuery.SearchMaxLength)
            .WithMessage($"La búsqueda no puede exceder {GetAdminProductsQuery.SearchMaxLength} caracteres.");

        RuleFor(x => x.Agotados)
            .Must(a => string.IsNullOrWhiteSpace(a)
                || a.Trim().ToLowerInvariant() is GetAdminProductsQuery.AgotadosCompleto or GetAdminProductsQuery.AgotadosAlguno)
            .WithMessage("El filtro de agotados debe ser 'completo' o 'alguno'.");
    }
}

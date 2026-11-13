using FluentValidation;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAllOrders;

public sealed class GetAllOrdersQueryValidator : AbstractValidator<GetAllOrdersQuery>
{
    public GetAllOrdersQueryValidator()
    {
        RuleFor(x => x.Hasta)
            .GreaterThanOrEqualTo(x => x.Desde)
            .When(x => x.Desde.HasValue && x.Hasta.HasValue)
            .WithMessage("La fecha 'hasta' no puede ser anterior a 'desde'.");

        RuleFor(x => x.Search)
            .MaximumLength(GetAllOrdersQuery.SearchMaxLength)
            .WithMessage($"La búsqueda no puede exceder {GetAllOrdersQuery.SearchMaxLength} caracteres.");
    }
}

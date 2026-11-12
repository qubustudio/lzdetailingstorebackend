using FluentValidation;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.ReorderProductRelations;

public sealed class ReorderProductRelationsCommandValidator : AbstractValidator<ReorderProductRelationsCommand>
{
    public ReorderProductRelationsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Tipo)
            .Must(t => ProductRelationLabels.TryParseType(t, out _))
            .WithMessage($"El tipo debe ser uno de: {string.Join(", ", ProductRelationLabels.TypeValues)}.");

        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("Debes enviar al menos una recomendación.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("La lista de recomendaciones contiene ids repetidos.");
    }
}

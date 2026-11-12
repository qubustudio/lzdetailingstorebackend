using FluentValidation;

namespace LzDetailingStore.Application.Features.Recommendations.Queries.GetProductRelations;

public sealed class GetProductRelationsQueryValidator : AbstractValidator<GetProductRelationsQuery>
{
    public GetProductRelationsQueryValidator()
    {
        RuleFor(x => x.Estado)
            .Must(e => ProductRelationLabels.TryParseStatus(e, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Estado))
            .WithMessage($"El estado debe ser uno de: {string.Join(", ", ProductRelationLabels.StatusValues)}.");

        RuleFor(x => x.Tipo)
            .Must(t => ProductRelationLabels.TryParseType(t, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Tipo))
            .WithMessage($"El tipo debe ser uno de: {string.Join(", ", ProductRelationLabels.TypeValues)}.");
    }
}

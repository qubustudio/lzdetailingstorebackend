using FluentValidation;
using LzDetailingStore.Domain.Entities.Catalog;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.UpdateProductRelation;

public sealed class UpdateProductRelationCommandValidator : AbstractValidator<UpdateProductRelationCommand>
{
    public UpdateProductRelationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.Status is not null || x.Reason is not null)
            .WithName("body")
            .WithMessage("Debes enviar al menos el estado o la razón.");

        RuleFor(x => x.Status)
            .Must(s => ProductRelationLabels.TryParseStatus(s, out _))
            .When(x => x.Status is not null)
            .WithMessage($"El estado debe ser uno de: {string.Join(", ", ProductRelationLabels.StatusValues)}.");

        RuleFor(x => x.Reason)
            .MaximumLength(ProductRelation.ReasonMaxLength)
            .WithMessage($"La razón no puede exceder {ProductRelation.ReasonMaxLength} caracteres.");
    }
}

using FluentValidation;
using LzDetailingStore.Domain.Entities.Catalog;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.CreateProductRelation;

public sealed class CreateProductRelationCommandValidator : AbstractValidator<CreateProductRelationCommand>
{
    public CreateProductRelationCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("El producto es requerido.");

        RuleFor(x => x.RelatedProductId)
            .NotEmpty().WithMessage("El producto relacionado es requerido.")
            .NotEqual(x => x.ProductId).WithMessage("Un producto no puede recomendarse a sí mismo.");

        RuleFor(x => x.Tipo)
            .Must(t => ProductRelationLabels.TryParseType(t, out _))
            .WithMessage($"El tipo debe ser uno de: {string.Join(", ", ProductRelationLabels.TypeValues)}.");

        RuleFor(x => x.Reason)
            .MaximumLength(ProductRelation.ReasonMaxLength)
            .WithMessage($"La razón no puede exceder {ProductRelation.ReasonMaxLength} caracteres.");
    }
}

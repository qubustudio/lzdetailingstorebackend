using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSpecs;

public sealed class ManageProductSpecsCommandValidator : AbstractValidator<ManageProductSpecsCommand>
{
    public ManageProductSpecsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Specs).NotNull();

        RuleForEach(x => x.Specs).ChildRules(spec =>
        {
            spec.RuleFor(s => s.Label)
                .NotEmpty().WithMessage("La etiqueta es requerida.")
                .MaximumLength(100);

            spec.RuleFor(s => s.Value)
                .NotEmpty().WithMessage("El valor es requerido.")
                .MaximumLength(500);

            spec.RuleFor(s => s.Orden)
                .GreaterThanOrEqualTo(0);
        });
    }
}

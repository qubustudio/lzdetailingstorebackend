using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSteps;

public sealed class ManageProductStepsCommandValidator : AbstractValidator<ManageProductStepsCommand>
{
    public ManageProductStepsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Steps).NotNull();

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.StepNumber)
                .GreaterThan(0).WithMessage("El número de paso debe ser mayor a 0.");

            step.RuleFor(s => s.Title)
                .NotEmpty().WithMessage("El título es requerido.")
                .MaximumLength(200);

            step.RuleFor(s => s.Description)
                .NotEmpty().WithMessage("La descripción es requerida.")
                .MaximumLength(1000);
        });
    }
}

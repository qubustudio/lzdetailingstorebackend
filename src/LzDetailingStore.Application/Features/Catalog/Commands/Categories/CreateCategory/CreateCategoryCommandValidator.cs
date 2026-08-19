using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => x.Descripcion is not null);

        RuleFor(x => x.Orden)
            .GreaterThanOrEqualTo(0).WithMessage("El orden debe ser mayor o igual a 0.");
    }
}

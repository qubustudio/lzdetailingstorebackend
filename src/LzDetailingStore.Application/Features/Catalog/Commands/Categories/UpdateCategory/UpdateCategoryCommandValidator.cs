using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).When(x => x.Descripcion is not null);

        RuleFor(x => x.Orden)
            .GreaterThanOrEqualTo(0).WithMessage("El orden debe ser mayor o igual a 0.");
    }
}

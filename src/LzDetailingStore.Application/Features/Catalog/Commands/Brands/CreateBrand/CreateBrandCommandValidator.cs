using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Brands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(1000).When(x => x.Descripcion is not null);

        RuleFor(x => x.Categoria)
            .MaximumLength(50).When(x => x.Categoria is not null);

        RuleFor(x => x.Origen)
            .MaximumLength(100).When(x => x.Origen is not null);

        RuleFor(x => x.Especialidad)
            .MaximumLength(200).When(x => x.Especialidad is not null);
    }
}

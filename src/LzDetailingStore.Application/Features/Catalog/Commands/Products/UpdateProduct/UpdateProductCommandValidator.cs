using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => x.Description is not null);

        RuleFor(x => x.BrandId).NotEmpty().WithMessage("La marca es requerida.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("La categoría es requerida.");

        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("La imagen es requerida.")
            .MaximumLength(500);
    }
}

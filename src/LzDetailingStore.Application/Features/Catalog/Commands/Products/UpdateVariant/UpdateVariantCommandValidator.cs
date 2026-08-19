using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;

public sealed class UpdateVariantCommandValidator : AbstractValidator<UpdateVariantCommand>
{
    public UpdateVariantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("El SKU es requerido.")
            .MaximumLength(50).WithMessage("El SKU no puede exceder 50 caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.PrecioVenta)
            .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0.");

        RuleFor(x => x.PrecioCosto)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de costo no puede ser negativo.");

        RuleFor(x => x.SizeValue)
            .GreaterThan(0).WithMessage("El tamaño debe ser mayor a 0.");

        RuleFor(x => x.SizeUnit)
            .NotEmpty().WithMessage("La unidad de medida es requerida.")
            .MaximumLength(20);

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");
    }
}

using FluentValidation;

namespace LzDetailingStore.Application.Features.Commerce.Commands.AddToCart;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
    }
}

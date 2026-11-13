using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.SetVariantAvailability;

public sealed class SetVariantAvailabilityCommandValidator : AbstractValidator<SetVariantAvailabilityCommand>
{
    public SetVariantAvailabilityCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.VariantId).NotEmpty();
    }
}

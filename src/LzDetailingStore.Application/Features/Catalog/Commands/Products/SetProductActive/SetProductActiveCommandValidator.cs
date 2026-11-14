using FluentValidation;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.SetProductActive;

public sealed class SetProductActiveCommandValidator : AbstractValidator<SetProductActiveCommand>
{
    public SetProductActiveCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

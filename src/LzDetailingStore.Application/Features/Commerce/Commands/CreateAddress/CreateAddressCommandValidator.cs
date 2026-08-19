using FluentValidation;

namespace LzDetailingStore.Application.Features.Commerce.Commands.CreateAddress;

public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Calle).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NumeroExterior).NotEmpty().MaximumLength(20);
        RuleFor(x => x.NumeroInterior).MaximumLength(20).When(x => x.NumeroInterior is not null);
        RuleFor(x => x.Colonia).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ciudad).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Estado).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CodigoPostal).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Telefono).MaximumLength(20).When(x => x.Telefono is not null);
        RuleFor(x => x.Referencia).MaximumLength(300).When(x => x.Referencia is not null);
    }
}

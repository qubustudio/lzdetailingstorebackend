using FluentValidation;

namespace LzDetailingStore.Application.Features.Content.Commands.Branches.CreateBranch;

public sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Direccion).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Telefono).MaximumLength(20).When(x => x.Telefono is not null);
        RuleFor(x => x.GoogleMapsUrl).MaximumLength(500).When(x => x.GoogleMapsUrl is not null);
        RuleFor(x => x.Horario).MaximumLength(200).When(x => x.Horario is not null);
    }
}

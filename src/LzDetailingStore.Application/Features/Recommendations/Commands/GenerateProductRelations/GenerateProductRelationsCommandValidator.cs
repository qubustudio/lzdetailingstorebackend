using FluentValidation;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.GenerateProductRelations;

public sealed class GenerateProductRelationsCommandValidator : AbstractValidator<GenerateProductRelationsCommand>
{
    public GenerateProductRelationsCommandValidator()
    {
        RuleFor(x => x.Mode).IsInEnum().WithMessage("El modo de generación no es válido.");
    }
}

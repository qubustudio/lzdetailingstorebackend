using FluentValidation;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.EnqueueProductRelationGeneration;

public sealed class EnqueueProductRelationGenerationCommandValidator
    : AbstractValidator<EnqueueProductRelationGenerationCommand>
{
    public EnqueueProductRelationGenerationCommandValidator()
    {
        RuleFor(x => x.Mode)
            .NotEmpty().WithMessage("El modo es requerido.")
            .Must(m => ProductRelationLabels.TryParseMode(m, out _))
            .WithMessage($"El modo debe ser uno de: {string.Join(", ", ProductRelationLabels.ModeValues)}.");
    }
}

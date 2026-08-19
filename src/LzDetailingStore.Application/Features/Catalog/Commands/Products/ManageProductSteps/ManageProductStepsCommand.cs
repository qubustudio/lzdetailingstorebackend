using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSteps;

public sealed record ProductStepItem(int StepNumber, string Title, string Description);

public sealed record ManageProductStepsCommand(
    Guid ProductId,
    IList<ProductStepItem> Steps) : IRequest<Result>;

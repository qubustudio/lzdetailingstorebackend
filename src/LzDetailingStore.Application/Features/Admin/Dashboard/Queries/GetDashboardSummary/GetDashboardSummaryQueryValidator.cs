using FluentValidation;

namespace LzDetailingStore.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.Dias)
            .InclusiveBetween(GetDashboardSummaryQuery.MinDias, GetDashboardSummaryQuery.MaxDias)
            .WithMessage($"El parámetro dias debe estar entre {GetDashboardSummaryQuery.MinDias} y {GetDashboardSummaryQuery.MaxDias}.");
    }
}

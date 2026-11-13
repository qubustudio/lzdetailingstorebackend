using LzDetailingStore.Application.Features.Admin.Dashboard.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;

/// <param name="Dias">Size of the reporting period in days, including today (7..365).</param>
/// <param name="IncluyeVentas">
/// Whether the caller holds <see cref="SalesPermission"/>. Resolved by the API layer; defaults to false so
/// money figures are only returned when explicitly allowed.
/// </param>
public sealed record GetDashboardSummaryQuery(
    int Dias = GetDashboardSummaryQuery.DefaultDias,
    bool IncluyeVentas = false)
    : IRequest<Result<DashboardSummaryDto>>
{
    public const int DefaultDias = 30;
    public const int MinDias = 7;
    public const int MaxDias = 365;

    /// <summary>Permission that unlocks sales/revenue figures in the dashboard summary.</summary>
    public const string SalesPermission = "dashboard.ventas";
}

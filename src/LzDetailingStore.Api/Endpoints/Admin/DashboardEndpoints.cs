using System.Security.Claims;
using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LzDetailingStore.Api.Endpoints.Admin;

public sealed class DashboardEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/dashboard")
            .WithTags("Admin - Dashboard")
            .RequireAuthorization("dashboard.ver");

        group.MapGet("/resumen", GetSummary);
    }

    private static async Task<IResult> GetSummary(
        ClaimsPrincipal user, IAuthorizationService authorization, ISender sender, CancellationToken ct,
        int dias = GetDashboardSummaryQuery.DefaultDias)
    {
        // Money figures require an extra permission; the permission policy provider resolves it
        // exactly like RequireAuthorization does (Operativo sees counts only).
        var sales = await authorization.AuthorizeAsync(user, GetDashboardSummaryQuery.SalesPermission);

        var result = await sender.Send(new GetDashboardSummaryQuery(dias, sales.Succeeded), ct);
        return result.ToApiResponse();
    }
}

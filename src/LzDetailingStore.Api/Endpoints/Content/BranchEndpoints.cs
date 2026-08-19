using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Content.Queries.GetBranches;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Content;

public sealed class BranchEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sucursales").WithTags("Branches");

        group.MapGet("/", GetBranches);
    }

    private static async Task<IResult> GetBranches(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetBranchesQuery(), ct);
        return result.ToApiResponse();
    }
}

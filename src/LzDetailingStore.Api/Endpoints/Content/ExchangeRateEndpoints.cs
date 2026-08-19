using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.ExchangeRate.Queries.GetExchangeRate;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Content;

public sealed class ExchangeRateEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tipo-cambio").WithTags("Exchange Rate");

        group.MapGet("/", GetExchangeRate);
    }

    private static async Task<IResult> GetExchangeRate(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetExchangeRateQuery(), ct);
        return result.ToApiResponse();
    }
}

using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Identity.Commands.Login;
using LzDetailingStore.Application.Features.Identity.Commands.RefreshToken;
using LzDetailingStore.Application.Features.Identity.Commands.RegisterUser;
using LzDetailingStore.Application.Features.Identity.Queries.GetProfile;
using MediatR;
using System.Security.Claims;

namespace LzDetailingStore.Api.Endpoints.Identity;

public sealed class AuthEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/registro", Register);
        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);
        group.MapGet("/perfil", GetProfile).RequireAuthorization();
    }

    private static async Task<IResult> Register(
        RegisterUserCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> Login(
        LoginCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> Refresh(
        RefreshTokenCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetProfile(
        ISender sender, HttpContext httpContext, CancellationToken ct)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? httpContext.User.FindFirstValue("sub");

        if (userId is null)
            return Results.Unauthorized();

        var result = await sender.Send(new GetProfileQuery(Guid.Parse(userId)), ct);
        return result.ToApiResponse();
    }
}

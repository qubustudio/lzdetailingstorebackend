namespace LzDetailingStore.Application.Features.Identity.DTOs;

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

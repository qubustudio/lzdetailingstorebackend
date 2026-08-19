using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token) : IRequest<Result<AuthResponseDto>>;

using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Identity.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponseDto>>;

using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Identity.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string Nombre,
    string? Telefono) : IRequest<Result<AuthResponseDto>>;

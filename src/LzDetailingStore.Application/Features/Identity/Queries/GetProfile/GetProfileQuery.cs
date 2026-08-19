using LzDetailingStore.Application.Features.Identity.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Identity.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<Result<ProfileDto>>;

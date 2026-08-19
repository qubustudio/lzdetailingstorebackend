using LzDetailingStore.Application.Features.Content.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Queries.GetBranches;

public sealed record GetBranchesQuery : IRequest<Result<List<BranchDto>>>;

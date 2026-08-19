using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Content.Commands.Branches.DeleteBranch;

public sealed record DeleteBranchCommand(Guid Id) : IRequest<Result>;

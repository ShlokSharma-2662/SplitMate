using FluentValidation;
using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;

namespace SplitMate.Application.Groups;

public record AddMemberCommand(Guid GroupId, string Email, string CurrentUserId) : IRequest<Result>;

public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.");
        RuleFor(x => x.CurrentUserId).NotEmpty();
    }
}

public class AddMemberCommandHandler(IGroupRepository groups, IUserDirectory users)
    : IRequestHandler<AddMemberCommand, Result>
{
    public async Task<Result> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await groups.GetWithMembersAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure("Group not found.");
        }

        if (group.Members.All(m => m.UserId != request.CurrentUserId))
        {
            return Result.Failure("You must be a member of this group to add members.");
        }

        var user = await users.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return Result.Failure($"No registered user found with email '{request.Email.Trim()}'.");
        }

        if (group.Members.Any(m => m.UserId == user.UserId))
        {
            return Result.Failure($"{user.DisplayName} is already a member of this group.");
        }

        await groups.AddMemberAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId = user.UserId,
            JoinedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return Result.Success();
    }
}

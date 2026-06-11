using FluentValidation;
using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;

namespace SplitMate.Application.Groups;

public record CreateGroupCommand(string Name, string CurrentUserId) : IRequest<Result<Guid>>;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(100).WithMessage("Group name must be 100 characters or fewer.");
        RuleFor(x => x.CurrentUserId).NotEmpty();
    }
}

public class CreateGroupCommandHandler(IGroupRepository groups)
    : IRequestHandler<CreateGroupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedByUserId = request.CurrentUserId,
            CreatedAtUtc = now
        };
        group.Members.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = request.CurrentUserId,
            JoinedAtUtc = now
        });

        await groups.AddAsync(group, cancellationToken);
        return Result<Guid>.Success(group.Id);
    }
}

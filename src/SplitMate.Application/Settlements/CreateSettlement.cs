using FluentValidation;
using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;

namespace SplitMate.Application.Settlements;

public record CreateSettlementCommand(
    Guid GroupId,
    string FromUserId,
    string ToUserId,
    decimal Amount,
    DateOnly Date,
    string? Note,
    string CurrentUserId) : IRequest<Result<Guid>>;

public class CreateSettlementCommandValidator : AbstractValidator<CreateSettlementCommand>
{
    public CreateSettlementCommandValidator(IGroupRepository groups)
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.FromUserId).NotEmpty().WithMessage("Select who paid.");
        RuleFor(x => x.ToUserId).NotEmpty().WithMessage("Select who received the payment.");
        RuleFor(x => x.CurrentUserId).NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Amount must be greater than zero.")
            .Must(a => a == Math.Round(a, 2)).WithMessage("Amount cannot have more than 2 decimal places.");

        RuleFor(x => x)
            .Must(x => x.FromUserId != x.ToUserId)
            .WithMessage("Payer and receiver must be different members.")
            .When(x => !string.IsNullOrEmpty(x.FromUserId) && !string.IsNullOrEmpty(x.ToUserId));

        RuleFor(x => x.Note).MaximumLength(200).WithMessage("Note must be 200 characters or fewer.");

        RuleFor(x => x)
            .CustomAsync(async (cmd, context, ct) =>
            {
                var memberIds = (await groups.GetMemberUserIdsAsync(cmd.GroupId, ct))
                    .ToHashSet(StringComparer.Ordinal);
                if (memberIds.Count == 0)
                {
                    context.AddFailure("Group not found.");
                    return;
                }

                if (!string.IsNullOrEmpty(cmd.CurrentUserId) && !memberIds.Contains(cmd.CurrentUserId))
                {
                    context.AddFailure("You must be a member of this group.");
                }

                if (!string.IsNullOrEmpty(cmd.FromUserId) && !memberIds.Contains(cmd.FromUserId))
                {
                    context.AddFailure("The payer must be a member of the group.");
                }

                if (!string.IsNullOrEmpty(cmd.ToUserId) && !memberIds.Contains(cmd.ToUserId))
                {
                    context.AddFailure("The receiver must be a member of the group.");
                }
            })
            .When(x => x.GroupId != Guid.Empty);
    }
}

public class CreateSettlementCommandHandler(ISettlementRepository settlements)
    : IRequestHandler<CreateSettlementCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateSettlementCommand request, CancellationToken cancellationToken)
    {
        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            FromUserId = request.FromUserId,
            ToUserId = request.ToUserId,
            Amount = request.Amount,
            Date = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await settlements.AddAsync(settlement, cancellationToken);
        return Result<Guid>.Success(settlement.Id);
    }
}

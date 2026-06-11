using FluentValidation;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Enums;
using SplitMate.Domain.Services;

namespace SplitMate.Application.Expenses;

/// <summary>
/// One participant of a split. <see cref="Value"/> is unused for Equal splits,
/// the exact share amount for ExactAmounts, and the percentage for Percentage splits.
/// </summary>
public record SplitParticipantInput(string UserId, decimal? Value);

/// <summary>Fields shared by create and update expense commands so validation rules can be shared.</summary>
public interface IExpenseInput
{
    Guid GroupId { get; }
    string Description { get; }
    decimal Amount { get; }
    string PaidByUserId { get; }
    SplitType SplitType { get; }
    IReadOnlyList<SplitParticipantInput> Participants { get; }
    string CurrentUserId { get; }
}

/// <summary>Shared validation rules for creating/updating an expense.</summary>
public abstract class ExpenseInputValidator<T> : AbstractValidator<T> where T : IExpenseInput
{
    protected ExpenseInputValidator(IGroupRepository groups)
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description must be 200 characters or fewer.");

        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Amount must be greater than zero.")
            .Must(a => a == Math.Round(a, 2)).WithMessage("Amount cannot have more than 2 decimal places.");

        RuleFor(x => x.PaidByUserId).NotEmpty().WithMessage("Select who paid.");
        RuleFor(x => x.CurrentUserId).NotEmpty();

        RuleFor(x => x.Participants)
            .NotEmpty().WithMessage("Select at least one participant.")
            .Must(p => p.Select(i => i.UserId).Distinct().Count() == p.Count)
            .WithMessage("Each participant can appear only once.");

        When(x => x.SplitType == SplitType.ExactAmounts && x.Participants.Count > 0, () =>
        {
            RuleFor(x => x.Participants)
                .Must(p => p.All(i => i.Value is > 0m))
                .WithMessage("Each exact share must be greater than zero.")
                .Must(p => p.All(i => i.Value is null || i.Value == Math.Round(i.Value.Value, 2)))
                .WithMessage("Shares cannot have more than 2 decimal places.");
            RuleFor(x => x)
                .Must(x => x.Participants.Sum(p => p.Value ?? 0m) == x.Amount)
                .WithMessage("Exact shares must sum to exactly the expense amount.")
                .OverridePropertyName(nameof(IExpenseInput.Participants));
        });

        When(x => x.SplitType == SplitType.Percentage && x.Participants.Count > 0, () =>
        {
            RuleFor(x => x.Participants)
                .Must(p => p.All(i => i.Value is > 0m))
                .WithMessage("Each percentage must be greater than zero.");
            RuleFor(x => x)
                .Must(x => x.Participants.Sum(p => p.Value ?? 0m) == 100m)
                .WithMessage("Percentages must sum to exactly 100.")
                .OverridePropertyName(nameof(IExpenseInput.Participants));
        });

        // Membership rules need the database, so they run async; one query covers
        // the payer, the participants and the acting user.
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

                if (!string.IsNullOrEmpty(cmd.PaidByUserId) && !memberIds.Contains(cmd.PaidByUserId))
                {
                    context.AddFailure("The payer must be a member of the group.");
                }

                if (cmd.Participants.Any(p => !memberIds.Contains(p.UserId)))
                {
                    context.AddFailure("All participants must be members of the group.");
                }
            })
            .When(x => x.GroupId != Guid.Empty);
    }
}

/// <summary>Computes the persisted share amounts for a validated expense input.</summary>
internal static class ShareComputation
{
    public static IReadOnlyList<(string UserId, decimal Amount)> Compute(
        decimal amount, SplitType splitType, IReadOnlyList<SplitParticipantInput> participants)
        => splitType switch
        {
            SplitType.Equal => MoneySplitter.SplitEqual(
                amount, participants.Select(p => p.UserId).ToList()),
            SplitType.ExactAmounts => participants
                .Select(p => (p.UserId, p.Value ?? 0m))
                .ToList(),
            SplitType.Percentage => MoneySplitter.SplitByPercentage(
                amount, participants.Select(p => (p.UserId, p.Value ?? 0m)).ToList()),
            _ => throw new ArgumentOutOfRangeException(nameof(splitType))
        };
}

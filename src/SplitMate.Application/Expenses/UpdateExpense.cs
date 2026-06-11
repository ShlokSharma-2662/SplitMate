using FluentValidation;
using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;
using SplitMate.Domain.Enums;

namespace SplitMate.Application.Expenses;

public record UpdateExpenseCommand(
    Guid ExpenseId,
    Guid GroupId,
    string Description,
    decimal Amount,
    string PaidByUserId,
    DateOnly Date,
    SplitType SplitType,
    IReadOnlyList<SplitParticipantInput> Participants,
    string CurrentUserId) : IRequest<Result>, IExpenseInput;

public class UpdateExpenseCommandValidator : ExpenseInputValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator(IGroupRepository groups) : base(groups)
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
    }
}

public class UpdateExpenseCommandHandler(IExpenseRepository expenses)
    : IRequestHandler<UpdateExpenseCommand, Result>
{
    public async Task<Result> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var existing = await expenses.GetWithSharesAsync(request.ExpenseId, cancellationToken);
        if (existing is null)
        {
            return Result.Failure("Expense not found.");
        }

        if (existing.GroupId != request.GroupId)
        {
            return Result.Failure("Expense does not belong to this group.");
        }

        if (existing.PaidByUserId != request.CurrentUserId)
        {
            return Result.Failure("You can only edit expenses you paid for.");
        }

        var updated = new Expense
        {
            Id = existing.Id,
            GroupId = existing.GroupId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            PaidByUserId = request.PaidByUserId,
            Date = request.Date,
            SplitType = request.SplitType,
            CreatedAtUtc = existing.CreatedAtUtc
        };

        foreach (var (userId, amount) in ShareComputation.Compute(
            request.Amount, request.SplitType, request.Participants))
        {
            updated.Shares.Add(new ExpenseShare
            {
                ExpenseId = updated.Id,
                UserId = userId,
                ShareAmount = amount
            });
        }

        await expenses.UpdateAsync(updated, cancellationToken);
        return Result.Success();
    }
}

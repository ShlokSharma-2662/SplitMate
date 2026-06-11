using FluentValidation;
using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;
using SplitMate.Domain.Enums;

namespace SplitMate.Application.Expenses;

public record CreateExpenseCommand(
    Guid GroupId,
    string Description,
    decimal Amount,
    string PaidByUserId,
    DateOnly Date,
    SplitType SplitType,
    IReadOnlyList<SplitParticipantInput> Participants,
    string CurrentUserId) : IRequest<Result<Guid>>, IExpenseInput;

public class CreateExpenseCommandValidator : ExpenseInputValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator(IGroupRepository groups) : base(groups)
    {
        RuleFor(x => x.GroupId).NotEmpty();
    }
}

public class CreateExpenseCommandHandler(IExpenseRepository expenses)
    : IRequestHandler<CreateExpenseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            PaidByUserId = request.PaidByUserId,
            Date = request.Date,
            SplitType = request.SplitType,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var (userId, amount) in ShareComputation.Compute(
            request.Amount, request.SplitType, request.Participants))
        {
            expense.Shares.Add(new ExpenseShare
            {
                ExpenseId = expense.Id,
                UserId = userId,
                ShareAmount = amount
            });
        }

        await expenses.AddAsync(expense, cancellationToken);
        return Result<Guid>.Success(expense.Id);
    }
}

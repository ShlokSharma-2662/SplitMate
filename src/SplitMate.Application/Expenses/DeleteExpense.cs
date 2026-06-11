using FluentValidation;
using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;

namespace SplitMate.Application.Expenses;

public record DeleteExpenseCommand(Guid ExpenseId, string CurrentUserId) : IRequest<Result>;

public class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
{
    public DeleteExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.CurrentUserId).NotEmpty();
    }
}

public class DeleteExpenseCommandHandler(IExpenseRepository expenses)
    : IRequestHandler<DeleteExpenseCommand, Result>
{
    public async Task<Result> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await expenses.GetWithSharesAsync(request.ExpenseId, cancellationToken);
        if (expense is null)
        {
            return Result.Failure("Expense not found.");
        }

        if (expense.PaidByUserId != request.CurrentUserId)
        {
            return Result.Failure("You can only delete expenses you paid for.");
        }

        await expenses.DeleteAsync(expense.Id, cancellationToken);
        return Result.Success();
    }
}

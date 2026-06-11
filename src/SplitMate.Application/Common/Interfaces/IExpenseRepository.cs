using SplitMate.Domain.Entities;

namespace SplitMate.Application.Common.Interfaces;

public interface IExpenseRepository
{
    Task<Expense?> GetWithSharesAsync(Guid expenseId, CancellationToken ct = default);

    /// <summary>All expenses of a group including shares, newest first.</summary>
    Task<IReadOnlyList<Expense>> GetForGroupAsync(Guid groupId, CancellationToken ct = default);

    Task AddAsync(Expense expense, CancellationToken ct = default);

    /// <summary>Persists new scalar values and reconciles the share rows of an existing expense.</summary>
    Task UpdateAsync(Expense expense, CancellationToken ct = default);

    Task DeleteAsync(Guid expenseId, CancellationToken ct = default);
}

using Microsoft.EntityFrameworkCore;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;

namespace SplitMate.Infrastructure.Persistence.Repositories;

public class ExpenseRepository(IDbContextFactory<SplitMateDbContext> factory) : IExpenseRepository
{
    public async Task<Expense?> GetWithSharesAsync(Guid expenseId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Expenses.AsNoTracking()
            .Include(e => e.Shares)
            .FirstOrDefaultAsync(e => e.Id == expenseId, ct);
    }

    public async Task<IReadOnlyList<Expense>> GetForGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Expenses.AsNoTracking()
            .Include(e => e.Shares)
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Expense expense, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Expense expense, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.Expenses
            .Include(e => e.Shares)
            .FirstOrDefaultAsync(e => e.Id == expense.Id, ct)
            ?? throw new InvalidOperationException($"Expense {expense.Id} not found.");

        existing.Description = expense.Description;
        existing.Amount = expense.Amount;
        existing.PaidByUserId = expense.PaidByUserId;
        existing.Date = expense.Date;
        existing.SplitType = expense.SplitType;

        // Reconcile shares in place to avoid tracking two instances with the same key.
        var obsolete = existing.Shares
            .Where(s => expense.Shares.All(n => n.UserId != s.UserId))
            .ToList();
        db.ExpenseShares.RemoveRange(obsolete);

        foreach (var share in expense.Shares)
        {
            var current = existing.Shares.FirstOrDefault(s => s.UserId == share.UserId);
            if (current is null)
            {
                existing.Shares.Add(new ExpenseShare
                {
                    ExpenseId = existing.Id,
                    UserId = share.UserId,
                    ShareAmount = share.ShareAmount
                });
            }
            else
            {
                current.ShareAmount = share.ShareAmount;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid expenseId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        // Shares are removed by the cascade on the ExpenseShare → Expense foreign key.
        await db.Expenses.Where(e => e.Id == expenseId).ExecuteDeleteAsync(ct);
    }
}

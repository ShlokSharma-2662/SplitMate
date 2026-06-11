namespace SplitMate.Domain.Entities;

/// <summary>One participant's owed portion of an expense. Composite key (ExpenseId, UserId).</summary>
public class ExpenseShare
{
    public Guid ExpenseId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal ShareAmount { get; set; }

    public Expense? Expense { get; set; }
}

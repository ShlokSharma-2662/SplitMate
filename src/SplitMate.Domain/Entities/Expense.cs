using SplitMate.Domain.Enums;

namespace SplitMate.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaidByUserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public SplitType SplitType { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Group? Group { get; set; }
    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
}

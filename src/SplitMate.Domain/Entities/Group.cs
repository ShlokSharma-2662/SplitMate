namespace SplitMate.Domain.Entities;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}

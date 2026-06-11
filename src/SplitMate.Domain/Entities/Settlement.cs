namespace SplitMate.Domain.Entities;

/// <summary>A repayment from one group member to another.</summary>
public class Settlement
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string FromUserId { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Note { get; set; }

    // Not in the spec's field list, but needed to order the dashboard activity
    // feed precisely (Date alone is day-granular). Documented in the README.
    public DateTime CreatedAtUtc { get; set; }

    public Group? Group { get; set; }
}

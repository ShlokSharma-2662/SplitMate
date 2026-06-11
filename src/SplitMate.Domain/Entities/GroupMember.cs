namespace SplitMate.Domain.Entities;

/// <summary>Membership of a user in a group. Composite key (GroupId, UserId).</summary>
public class GroupMember
{
    public Guid GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }

    public Group? Group { get; set; }
}

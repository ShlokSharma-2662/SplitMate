namespace SplitMate.Application.Common.Interfaces;

public record UserRef(string UserId, string DisplayName, string Email);

/// <summary>Read-only lookup of registered users (backed by ASP.NET Core Identity).</summary>
public interface IUserDirectory
{
    Task<UserRef?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, UserRef>> GetByIdsAsync(
        IEnumerable<string> userIds, CancellationToken ct = default);
}

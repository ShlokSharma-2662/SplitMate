using Microsoft.AspNetCore.Identity;

namespace SplitMate.Infrastructure.Identity;

/// <summary>
/// Application user. Lives in Infrastructure (not Domain) so the Domain layer
/// stays free of the ASP.NET Core Identity dependency; domain entities refer
/// to users by their string Id only.
/// </summary>
public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}

using System.Security.Claims;

namespace SplitMate.Web;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Current user has no id claim.");
}

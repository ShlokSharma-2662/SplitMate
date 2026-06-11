using System.Globalization;

namespace SplitMate.Web;

public static class MoneyFormat
{
    private static readonly CultureInfo Indian = CultureInfo.GetCultureInfo("en-IN");

    /// <summary>Formats an amount as ₹ with 2 decimals and Indian digit grouping (e.g. ₹1,23,456.78).</summary>
    public static string Inr(decimal amount) => amount.ToString("C2", Indian);
}

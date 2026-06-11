namespace SplitMate.Domain.Services;

/// <summary>A suggested repayment produced by <see cref="DebtSimplifier"/>.</summary>
public record SimplifiedDebt(string FromUserId, string ToUserId, decimal Amount);

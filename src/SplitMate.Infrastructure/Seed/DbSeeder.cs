using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SplitMate.Domain.Entities;
using SplitMate.Domain.Enums;
using SplitMate.Domain.Services;
using SplitMate.Infrastructure.Identity;
using SplitMate.Infrastructure.Persistence;

namespace SplitMate.Infrastructure.Seed;

/// <summary>
/// Seeds 3 demo users, 1 demo group and 4 expenses so the app is demonstrable
/// immediately after first run. Idempotent: skipped when any user already exists.
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo@Pass1";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SplitMateDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var aarav = await CreateUserAsync(userManager, "aarav@demo.com", "Aarav Sharma");
        var diya = await CreateUserAsync(userManager, "diya@demo.com", "Diya Patel");
        var rohan = await CreateUserAsync(userManager, "rohan@demo.com", "Rohan Mehta");
        var allIds = new[] { aarav.Id, diya.Id, rohan.Id };

        var now = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Goa Trip",
            CreatedByUserId = aarav.Id,
            CreatedAtUtc = now.AddDays(-7)
        };
        foreach (var id in allIds)
        {
            group.Members.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = id,
                JoinedAtUtc = now.AddDays(-7)
            });
        }

        db.Groups.Add(group);
        db.Expenses.AddRange(
            MakeExpense(group.Id, "Beach resort booking", 9000m, aarav.Id,
                now.AddDays(-6), SplitType.Equal,
                MoneySplitter.SplitEqual(9000m, allIds)),
            MakeExpense(group.Id, "Seafood dinner at beach shack", 2500m, diya.Id,
                now.AddDays(-5), SplitType.Equal,
                MoneySplitter.SplitEqual(2500m, allIds)),
            MakeExpense(group.Id, "Scooter rentals", 1200m, rohan.Id,
                now.AddDays(-4), SplitType.ExactAmounts,
                [(aarav.Id, 500m), (diya.Id, 400m), (rohan.Id, 300m)]),
            MakeExpense(group.Id, "Snacks and drinks", 450m, aarav.Id,
                now.AddDays(-3), SplitType.Percentage,
                MoneySplitter.SplitByPercentage(450m,
                    [(aarav.Id, 50m), (diya.Id, 30m), (rohan.Id, 20m)])));

        await db.SaveChangesAsync();
    }

    private static async Task<AppUser> CreateUserAsync(
        UserManager<AppUser> userManager, string email, string displayName)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user {email}: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    private static Expense MakeExpense(
        Guid groupId, string description, decimal amount, string paidByUserId,
        DateTime createdAtUtc, SplitType splitType,
        IReadOnlyList<(string UserId, decimal Amount)> shares)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Description = description,
            Amount = amount,
            PaidByUserId = paidByUserId,
            Date = DateOnly.FromDateTime(createdAtUtc),
            SplitType = splitType,
            CreatedAtUtc = createdAtUtc
        };
        foreach (var (userId, shareAmount) in shares)
        {
            expense.Shares.Add(new ExpenseShare
            {
                ExpenseId = expense.Id,
                UserId = userId,
                ShareAmount = shareAmount
            });
        }

        return expense;
    }
}

using FluentAssertions;
using SplitMate.Application.Expenses;
using SplitMate.Domain.Enums;

namespace SplitMate.Tests.Application;

public class CreateExpenseCommandValidatorTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private readonly CreateExpenseCommandValidator _validator =
        new(new FakeGroupRepository("u1", "u2", "u3"));

    private static CreateExpenseCommand ValidCommand() => new(
        GroupId,
        "Dinner",
        300m,
        PaidByUserId: "u1",
        Date: new DateOnly(2026, 6, 10),
        SplitType: SplitType.Equal,
        Participants: [new("u1", null), new("u2", null), new("u3", null)],
        CurrentUserId: "u1");

    [Fact]
    public async Task Valid_equal_split_passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12.50)]
    public async Task Non_positive_amount_fails(decimal amount)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Amount = amount });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Amount must be greater than zero.");
    }

    [Fact]
    public async Task Empty_description_fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Description = " " });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Description is required.");
    }

    [Fact]
    public async Task Exact_shares_must_sum_to_the_expense_amount()
    {
        var command = ValidCommand() with
        {
            SplitType = SplitType.ExactAmounts,
            Participants = [new("u1", 100m), new("u2", 100m), new("u3", 99.99m)]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Exact shares must sum to exactly the expense amount.");
    }

    [Fact]
    public async Task Exact_shares_summing_exactly_pass()
    {
        var command = ValidCommand() with
        {
            SplitType = SplitType.ExactAmounts,
            Participants = [new("u1", 100m), new("u2", 100.01m), new("u3", 99.99m)]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Percentages_must_sum_to_100()
    {
        var command = ValidCommand() with
        {
            SplitType = SplitType.Percentage,
            Participants = [new("u1", 50m), new("u2", 30m), new("u3", 19.99m)]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Percentages must sum to exactly 100.");
    }

    [Fact]
    public async Task Payer_outside_the_group_fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { PaidByUserId = "stranger" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The payer must be a member of the group.");
    }

    [Fact]
    public async Task Participant_outside_the_group_fails()
    {
        var command = ValidCommand() with
        {
            Participants = [new("u1", null), new("stranger", null)]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "All participants must be members of the group.");
    }

    [Fact]
    public async Task Duplicate_participants_fail()
    {
        var command = ValidCommand() with
        {
            Participants = [new("u1", null), new("u1", null)]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Each participant can appear only once.");
    }
}

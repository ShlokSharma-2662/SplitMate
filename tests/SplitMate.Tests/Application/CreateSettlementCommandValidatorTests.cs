using FluentAssertions;
using SplitMate.Application.Settlements;

namespace SplitMate.Tests.Application;

public class CreateSettlementCommandValidatorTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private readonly CreateSettlementCommandValidator _validator =
        new(new FakeGroupRepository("u1", "u2"));

    private static CreateSettlementCommand ValidCommand() => new(
        GroupId,
        FromUserId: "u2",
        ToUserId: "u1",
        Amount: 150.50m,
        Date: new DateOnly(2026, 6, 10),
        Note: "UPI transfer",
        CurrentUserId: "u1");

    [Fact]
    public async Task Valid_settlement_passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Non_positive_amount_fails(decimal amount)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Amount = amount });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Amount must be greater than zero.");
    }

    [Fact]
    public async Task Amount_with_more_than_two_decimals_fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Amount = 10.005m });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Amount cannot have more than 2 decimal places.");
    }

    [Fact]
    public async Task Paying_yourself_fails()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { FromUserId = "u1", ToUserId = "u1" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Payer and receiver must be different members.");
    }

    [Fact]
    public async Task Non_member_payer_fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { FromUserId = "stranger" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The payer must be a member of the group.");
    }
}

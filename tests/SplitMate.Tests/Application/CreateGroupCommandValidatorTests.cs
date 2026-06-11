using FluentAssertions;
using SplitMate.Application.Groups;

namespace SplitMate.Tests.Application;

public class CreateGroupCommandValidatorTests
{
    private readonly CreateGroupCommandValidator _validator = new();

    [Fact]
    public void Valid_group_passes()
    {
        var result = _validator.Validate(new CreateGroupCommand("Goa Trip", "u1"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_fails(string name)
    {
        var result = _validator.Validate(new CreateGroupCommand(name, "u1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Group name is required.");
    }

    [Fact]
    public void Name_longer_than_100_characters_fails()
    {
        var result = _validator.Validate(new CreateGroupCommand(new string('x', 101), "u1"));

        result.IsValid.Should().BeFalse();
    }
}

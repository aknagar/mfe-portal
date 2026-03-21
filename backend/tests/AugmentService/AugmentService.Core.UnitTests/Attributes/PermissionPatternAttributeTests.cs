using System.ComponentModel.DataAnnotations;
using AugmentService.Core.Attributes;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests.Attributes;

public class PermissionPatternAttributeTests
{
    private readonly PermissionPatternAttribute _sut = new();
    private readonly ValidationContext _ctx = new(new object());

    private ValidationResult? Validate(object? value)
        => _sut.GetValidationResult(value, _ctx);

    [Fact]
    public void IsValid_Should_ReturnSuccess_When_ValueIsNull()
    {
        var result = Validate(null);
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData("System.Read")]
    [InlineData("System.Write")]
    [InlineData("Augment.Admin")]
    [InlineData("A.B")]
    public void IsValid_Should_ReturnSuccess_When_StringMatchesPattern(string value)
    {
        var result = Validate(value);
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData("noperiod")]
    [InlineData("too.many.parts")]
    [InlineData(".leading")]
    [InlineData("trailing.")]
    [InlineData("has space.Read")]
    [InlineData("System.has space")]
    [InlineData("123.Read")]
    [InlineData("")]
    public void IsValid_Should_ReturnError_When_StringDoesNotMatchPattern(string value)
    {
        var result = Validate(value);
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().NotBeNullOrEmpty();
        if (!string.IsNullOrEmpty(value))
        {
            result.ErrorMessage.Should().Contain(value);
        }
    }

    [Fact]
    public void IsValid_Should_ReturnSuccess_When_AllStringsInCollectionMatchPattern()
    {
        var permissions = new List<string> { "System.Read", "System.Write", "Augment.Admin" };
        var result = Validate(permissions);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void IsValid_Should_ReturnError_When_SomeStringsInCollectionDoNotMatchPattern()
    {
        var permissions = new List<string> { "System.Read", "invalid", "noperiod" };
        var result = Validate(permissions);
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("invalid");
        result.ErrorMessage.Should().Contain("noperiod");
    }

    [Fact]
    public void IsValid_Should_ReturnError_When_ValueIsNonStringNonCollection()
    {
        var result = Validate(42);
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("string");
    }

    [Fact]
    public void ErrorMessage_Should_ContainPatternExample()
    {
        var attribute = new PermissionPatternAttribute();
        attribute.FormatErrorMessage("test").Should().Contain("Resource.Action");
    }

    [Fact]
    public void IsValid_Should_ReturnSuccess_When_EmptyCollection()
    {
        var result = Validate(new List<string>());
        result.Should().Be(ValidationResult.Success);
    }
}

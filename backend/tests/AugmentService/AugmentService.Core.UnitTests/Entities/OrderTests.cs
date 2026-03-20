using System.ComponentModel.DataAnnotations;
using AugmentService.Core.Entities;
using AutoFixture;
using Common.Fixtures;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests.Entities;

public class OrderTests
{
    private readonly IFixture _fixture;

    public OrderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new DomainCustomization());
    }

    [Fact]
    public void Should_CreateOrder_When_ValidDataProvided()
    {
        // Arrange & Act
        var order = new Order
        {
            Name = "Test Order",
            TotalCost = 1000,
            Quantity = 5
        };

        // Assert
        order.Should().NotBeNull();
        order.Name.Should().Be("Test Order");
        order.TotalCost.Should().Be(1000);
        order.Quantity.Should().Be(5);
    }

    [Fact]
    public void Should_SetName_When_RequiredPropertyProvided()
    {
        // Arrange & Act
        var order = new Order
        {
            Name = "Product Order"
        };

        // Assert
        order.Name.Should().Be("Product Order");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(5000)]
    [InlineData(999999)]
    public void Should_SetTotalCost_When_ValidAmountProvided(int totalCost)
    {
        // Arrange & Act
        var order = new Order
        {
            Name = "Test Order",
            TotalCost = totalCost
        };

        // Assert
        order.TotalCost.Should().Be(totalCost);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Should_SetQuantity_When_ValidQuantityProvided(int quantity)
    {
        // Arrange & Act
        var order = new Order
        {
            Name = "Test Order",
            Quantity = quantity
        };

        // Assert
        order.Quantity.Should().Be(quantity);
    }

    [Fact]
    public void Should_AllowZeroQuantity_When_OrderCreated()
    {
        // Arrange & Act
        var order = new Order
        {
            Name = "Test Order",
            Quantity = 0
        };

        // Assert
        order.Quantity.Should().Be(0);
    }

    [Fact]
    public void Should_UpdateName_When_PropertyChanged()
    {
        // Arrange
        var order = new Order
        {
            Name = "Original Order",
            TotalCost = 500,
            Quantity = 2
        };

        // Act
        order.Name = "Updated Order";

        // Assert
        order.Name.Should().Be("Updated Order");
    }

    [Fact]
    public void Should_UpdateTotalCost_When_PropertyChanged()
    {
        // Arrange
        var order = new Order
        {
            Name = "Test Order",
            TotalCost = 500
        };

        // Act
        order.TotalCost = 1500;

        // Assert
        order.TotalCost.Should().Be(1500);
    }

    [Fact]
    public void Should_UpdateQuantity_When_PropertyChanged()
    {
        // Arrange
        var order = new Order
        {
            Name = "Test Order",
            Quantity = 5
        };

        // Act
        order.Quantity = 10;

        // Assert
        order.Quantity.Should().Be(10);
    }

    [Fact]
    public void Should_CreateOrderWithAutoFixture_When_DomainCustomizationUsed()
    {
        // Arrange & Act
        var order = _fixture.Create<Order>();

        // Assert
        order.Should().NotBeNull();
        order.Name.Should().NotBeNullOrEmpty();
        order.TotalCost.Should().BeGreaterThan(0);
        order.Quantity.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_CreateMultipleUniqueOrders_When_AutoFixtureUsed()
    {
        // Arrange & Act
        var order1 = _fixture.Create<Order>();
        var order2 = _fixture.Create<Order>();

        // Assert
        order1.Should().NotBeNull();
        order2.Should().NotBeNull();
        order1.Name.Should().NotBe(order2.Name);
    }

    [Fact]
    public void Should_DefaultToZero_When_TotalCostNotSet()
    {
        // Arrange & Act
        var order = new Order
        {
            Name = "Test Order"
        };

        // Assert
        order.TotalCost.Should().Be(0);
    }

    [Fact]
    public void Should_DefaultToZero_When_QuantityNotSet()
    {
        // Arrange & Act
        var order = new Order
        {
            Name = "Test Order"
        };

        // Assert
        order.Quantity.Should().Be(0);
    }

    #region Data Annotation Validation Tests

    private static IList<ValidationResult> ValidateOrder(Order order)
    {
        var context = new ValidationContext(order);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(order, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Should_PassValidation_When_AllPropertiesAreValid()
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = 100, Quantity = 2 };

        // Act
        var results = ValidateOrder(order);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_FailValidation_When_NameIsEmptyOrWhitespace(string name)
    {
        // Arrange
        var order = new Order { Name = name, TotalCost = 100, Quantity = 1 };

        // Act
        var results = ValidateOrder(order);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Order.Name)));
    }

    [Fact]
    public void Should_FailValidation_When_QuantityIsZero()
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = 100, Quantity = 0 };

        // Act
        var results = ValidateOrder(order);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Order.Quantity)));
    }

    [Fact]
    public void Should_FailValidation_When_QuantityIsNegative()
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = 100, Quantity = -1 };

        // Act
        var results = ValidateOrder(order);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Order.Quantity)));
    }

    [Fact]
    public void Should_FailValidation_When_TotalCostIsNegative()
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = -1, Quantity = 1 };

        // Act
        var results = ValidateOrder(order);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(Order.TotalCost)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999999)]
    public void Should_PassValidation_When_TotalCostIsZeroOrPositive(int totalCost)
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = totalCost, Quantity = 1 };

        // Act
        var results = ValidateOrder(order);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public void Should_PassValidation_When_QuantityIsPositive(int quantity)
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = 50, Quantity = quantity };

        // Act
        var results = ValidateOrder(order);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion
}

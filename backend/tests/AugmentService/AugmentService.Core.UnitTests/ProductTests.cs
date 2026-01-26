using AugmentService.Core.Entities;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests;

public class ProductTests
{
    [Fact]
    public void Product_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 19.99m
        };

        // Assert
        product.Id.Should().Be(1);
        product.Name.Should().Be("Test Product");
        product.Price.Should().Be(19.99m);
        product.Description.Should().BeNull();
        product.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Product_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var product = new Product
        {
            Id = 42,
            Name = "Complete Product",
            Description = "A complete product with all fields",
            Price = 99.99m,
            ImageUrl = "https://example.com/image.png"
        };

        // Assert
        product.Id.Should().Be(42);
        product.Name.Should().Be("Complete Product");
        product.Description.Should().Be("A complete product with all fields");
        product.Price.Should().Be(99.99m);
        product.ImageUrl.Should().Be("https://example.com/image.png");
    }

    [Fact]
    public void Product_Price_CanBeZero()
    {
        // Arrange & Act
        var product = new Product
        {
            Id = 1,
            Name = "Free Product",
            Price = 0m
        };

        // Assert
        product.Price.Should().Be(0m);
    }

    [Fact]
    public void Product_Price_CanBeNegative()
    {
        // Arrange & Act (e.g., for refunds or discounts)
        var product = new Product
        {
            Id = 1,
            Name = "Discount",
            Price = -10.00m
        };

        // Assert
        product.Price.Should().Be(-10.00m);
    }
}

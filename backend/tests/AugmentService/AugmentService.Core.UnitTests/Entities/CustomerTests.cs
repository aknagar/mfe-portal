using AugmentService.Core.Entities;
using AutoFixture;
using Common.Fixtures;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests.Entities;

public class CustomerTests
{
    private readonly IFixture _fixture;

    public CustomerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new DomainCustomization());
    }

    [Fact]
    public void Should_CreateCustomer_When_ValidDataProvided()
    {
        // Arrange & Act
        var customer = new Customer
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        customer.Should().NotBeNull();
        customer.FirstName.Should().Be("John");
        customer.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Should_SetFirstName_When_RequiredPropertyProvided()
    {
        // Arrange & Act
        var customer = new Customer
        {
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Assert
        customer.FirstName.Should().Be("Jane");
    }

    [Fact]
    public void Should_SetLastName_When_RequiredPropertyProvided()
    {
        // Arrange & Act
        var customer = new Customer
        {
            FirstName = "Bob",
            LastName = "Johnson"
        };

        // Assert
        customer.LastName.Should().Be("Johnson");
    }

    [Fact]
    public void Should_UpdateFirstName_When_PropertyChanged()
    {
        // Arrange
        var customer = new Customer
        {
            FirstName = "Original",
            LastName = "Name"
        };

        // Act
        customer.FirstName = "Updated";

        // Assert
        customer.FirstName.Should().Be("Updated");
    }

    [Fact]
    public void Should_UpdateLastName_When_PropertyChanged()
    {
        // Arrange
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "Original"
        };

        // Act
        customer.LastName = "Updated";

        // Assert
        customer.LastName.Should().Be("Updated");
    }

    [Theory]
    [InlineData("Alice", "Anderson")]
    [InlineData("Bob", "Brown")]
    [InlineData("Charlie", "Chen")]
    [InlineData("Diana", "Davis")]
    public void Should_CreateCustomer_When_DifferentNamesProvided(string firstName, string lastName)
    {
        // Arrange & Act
        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Assert
        customer.FirstName.Should().Be(firstName);
        customer.LastName.Should().Be(lastName);
    }

    [Fact]
    public void Should_CreateCustomerWithAutoFixture_When_DomainCustomizationUsed()
    {
        // Arrange & Act
        var customer = _fixture.Create<Customer>();

        // Assert
        customer.Should().NotBeNull();
        customer.FirstName.Should().NotBeNullOrEmpty();
        customer.LastName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Should_CreateMultipleUniqueCustomers_When_AutoFixtureUsed()
    {
        // Arrange & Act
        var customer1 = _fixture.Create<Customer>();
        var customer2 = _fixture.Create<Customer>();

        // Assert
        customer1.Should().NotBeNull();
        customer2.Should().NotBeNull();
        customer1.FirstName.Should().NotBe(customer2.FirstName);
        customer1.LastName.Should().NotBe(customer2.LastName);
    }

    [Fact]
    public void Should_AllowSingleCharacterNames_When_CustomerCreated()
    {
        // Arrange & Act
        var customer = new Customer
        {
            FirstName = "A",
            LastName = "B"
        };

        // Assert
        customer.FirstName.Should().Be("A");
        customer.LastName.Should().Be("B");
    }

    [Fact]
    public void Should_AllowLongNames_When_CustomerCreated()
    {
        // Arrange
        var longFirstName = new string('A', 100);
        var longLastName = new string('B', 100);

        // Act
        var customer = new Customer
        {
            FirstName = longFirstName,
            LastName = longLastName
        };

        // Assert
        customer.FirstName.Should().HaveLength(100);
        customer.LastName.Should().HaveLength(100);
    }

    [Fact]
    public void Should_AllowSpecialCharactersInNames_When_CustomerCreated()
    {
        // Arrange & Act
        var customer = new Customer
        {
            FirstName = "Mary-Jane",
            LastName = "O'Brien"
        };

        // Assert
        customer.FirstName.Should().Be("Mary-Jane");
        customer.LastName.Should().Be("O'Brien");
    }

    [Fact]
    public void Should_AllowUnicodeCharacters_When_CustomerCreated()
    {
        // Arrange & Act
        var customer = new Customer
        {
            FirstName = "José",
            LastName = "Müller"
        };

        // Assert
        customer.FirstName.Should().Be("José");
        customer.LastName.Should().Be("Müller");
    }
}

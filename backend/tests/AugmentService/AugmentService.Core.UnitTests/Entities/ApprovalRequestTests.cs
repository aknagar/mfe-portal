using AugmentService.Core.Entities;
using AutoFixture;
using Common.Fixtures;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests.Entities;

public class ApprovalRequestTests
{
    private readonly IFixture _fixture;

    public ApprovalRequestTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new DomainCustomization());
    }

    [Fact]
    public void Should_CreateApprovalRequest_When_ValidDataProvided()
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-12345",
            OrderName = "Test Order",
            TotalCost = 1500.50,
            Quantity = 10,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest.Should().NotBeNull();
        approvalRequest.OrderId.Should().Be("ORD-12345");
        approvalRequest.OrderName.Should().Be("Test Order");
        approvalRequest.TotalCost.Should().Be(1500.50);
        approvalRequest.Quantity.Should().Be(10);
    }

    [Fact]
    public void Should_DefaultToPendingStatus_When_ApprovalRequestCreated()
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest.Status.Should().Be(ApprovalStatus.Pending);
    }

    [Fact]
    public void Should_SetCreatedAtToUtcNow_When_ApprovalRequestCreated()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var after = DateTime.UtcNow;

        // Assert
        approvalRequest.CreatedAt.Should().BeOnOrAfter(before);
        approvalRequest.CreatedAt.Should().BeOnOrBefore(after);
        approvalRequest.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Should_SetProcessedAtToNull_When_ApprovalRequestCreated()
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Should_SetProcessedByToNull_When_ApprovalRequestCreated()
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest.ProcessedBy.Should().BeNull();
    }

    [Fact]
    public void Should_SetCommentsToNull_When_ApprovalRequestCreated()
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest.Comments.Should().BeNull();
    }

    [Theory]
    [InlineData(ApprovalStatus.Pending)]
    [InlineData(ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.TimedOut)]
    public void Should_SetStatus_When_ValidStatusProvided(ApprovalStatus status)
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            Status = status
        };

        // Assert
        approvalRequest.Status.Should().Be(status);
    }

    [Fact]
    public void Should_UpdateToApproved_When_RequestIsApproved()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Act
        approvalRequest.Status = ApprovalStatus.Approved;
        approvalRequest.ProcessedAt = DateTime.UtcNow;
        approvalRequest.ProcessedBy = "admin@example.com";
        approvalRequest.Comments = "Approved for processing";

        // Assert
        approvalRequest.Status.Should().Be(ApprovalStatus.Approved);
        approvalRequest.ProcessedAt.Should().NotBeNull();
        approvalRequest.ProcessedBy.Should().Be("admin@example.com");
        approvalRequest.Comments.Should().Be("Approved for processing");
    }

    [Fact]
    public void Should_UpdateToRejected_When_RequestIsRejected()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Act
        approvalRequest.Status = ApprovalStatus.Rejected;
        approvalRequest.ProcessedAt = DateTime.UtcNow;
        approvalRequest.ProcessedBy = "manager@example.com";
        approvalRequest.Comments = "Insufficient budget";

        // Assert
        approvalRequest.Status.Should().Be(ApprovalStatus.Rejected);
        approvalRequest.ProcessedAt.Should().NotBeNull();
        approvalRequest.ProcessedBy.Should().Be("manager@example.com");
        approvalRequest.Comments.Should().Be("Insufficient budget");
    }

    [Fact]
    public void Should_UpdateToTimedOut_When_ExpirationTimeReached()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired 1 hour ago
        };

        // Act
        approvalRequest.Status = ApprovalStatus.TimedOut;

        // Assert
        approvalRequest.Status.Should().Be(ApprovalStatus.TimedOut);
    }

    [Fact]
    public void Should_SetExpiresAt_When_ValidDateProvided()
    {
        // Arrange
        var expirationDate = DateTime.UtcNow.AddHours(48);

        // Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            ExpiresAt = expirationDate
        };

        // Assert
        approvalRequest.ExpiresAt.Should().Be(expirationDate);
    }

    [Fact]
    public void Should_AllowMultipleCosts_When_DifferentOrdersCreated()
    {
        // Arrange & Act
        var approvalRequest1 = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Small Order",
            TotalCost = 100.00,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var approvalRequest2 = new ApprovalRequest
        {
            OrderId = "ORD-002",
            OrderName = "Large Order",
            TotalCost = 10000.00,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest1.TotalCost.Should().Be(100.00);
        approvalRequest2.TotalCost.Should().Be(10000.00);
    }

    [Fact]
    public void Should_CreateApprovalRequestWithAutoFixture_When_DomainCustomizationUsed()
    {
        // Arrange & Act
        var approvalRequest = _fixture.Create<ApprovalRequest>();

        // Assert
        approvalRequest.Should().NotBeNull();
        approvalRequest.OrderId.Should().NotBeNullOrEmpty();
        approvalRequest.OrderName.Should().NotBeNullOrEmpty();
        approvalRequest.Status.Should().Be(ApprovalStatus.Pending);
        approvalRequest.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        approvalRequest.ExpiresAt.Should().BeAfter(approvalRequest.CreatedAt);
    }

    [Fact]
    public void Should_CreateMultipleUniqueApprovalRequests_When_AutoFixtureUsed()
    {
        // Arrange & Act
        var request1 = _fixture.Create<ApprovalRequest>();
        var request2 = _fixture.Create<ApprovalRequest>();

        // Assert
        request1.Should().NotBeNull();
        request2.Should().NotBeNull();
        request1.OrderId.Should().NotBe(request2.OrderId);
        request1.OrderName.Should().NotBe(request2.OrderName);
    }

    [Fact]
    public void Should_AllowZeroTotalCost_When_ApprovalRequestCreated()
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Free Order",
            TotalCost = 0,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest.TotalCost.Should().Be(0);
    }

    [Fact]
    public void Should_AllowZeroQuantity_When_ApprovalRequestCreated()
    {
        // Arrange & Act
        var approvalRequest = new ApprovalRequest
        {
            OrderId = "ORD-001",
            OrderName = "Test Order",
            Quantity = 0,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Assert
        approvalRequest.Quantity.Should().Be(0);
    }
}

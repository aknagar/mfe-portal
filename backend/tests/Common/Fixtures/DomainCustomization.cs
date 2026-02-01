using AutoFixture;
using AugmentService.Core;
using AugmentService.Core.Entities;

namespace Common.Fixtures;

/// <summary>
/// AutoFixture customization for domain entities.
/// Ensures generated entities meet validation requirements.
/// </summary>
public class DomainCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Configure User entity generation
        fixture.Customize<User>(composer => composer
            .With(u => u.Email, () => $"test.{Guid.NewGuid()}@example.com") // Valid email format
            .With(u => u.CreatedDate, DateTime.UtcNow)
            .With(u => u.UpdatedDate, (DateTime?)null)
            .Without(u => u.UserRoles) // Avoid circular references
        );

        // Configure Role entity generation
        fixture.Customize<Role>(composer => composer
            .With(r => r.Name, () => $"Role_{Guid.NewGuid().ToString().Substring(0, 8)}") // Unique name
            .With(r => r.Description, "Auto-generated test role")
            .With(r => r.Permissions, new List<string> { "System.Read" }) // Valid permission
            .With(r => r.Rank, () => fixture.Create<int>() % 999 + 1) // Range 1-999
            .With(r => r.IsActive, true)
            .With(r => r.CreatedDate, DateTime.UtcNow)
            .With(r => r.UpdatedDate, (DateTime?)null)
            .Without(r => r.UserRoles) // Avoid circular references
        );

        // Configure Order entity generation
        fixture.Customize<Order>(composer => composer
            .With(o => o.Name, () => $"Order_{fixture.Create<int>()}")
            .With(o => o.TotalCost, () => fixture.Create<int>() % 10000 + 1) // Positive value
            .With(o => o.Quantity, () => fixture.Create<int>() % 100 + 1) // Positive value
        );

        // Configure UserRole entity generation
        fixture.Customize<UserRole>(composer => composer
            .With(ur => ur.CreatedDate, DateTime.UtcNow)
            .With(ur => ur.UpdatedDate, (DateTime?)null)
            .Without(ur => ur.User) // Avoid circular references
            .Without(ur => ur.Role) // Avoid circular references
        );

        // Configure Product entity generation
        fixture.Customize<Product>(composer => composer
            .With(p => p.Name, () => $"Product_{fixture.Create<int>()}")
            .With(p => p.Description, "Auto-generated test product")
            .With(p => p.Price, () => (decimal)(fixture.Create<int>() % 1000 + 1)) // Positive price
            .With(p => p.ImageUrl, () => $"https://example.com/image_{Guid.NewGuid()}.jpg")
        );

        // Configure ProxyTarget entity generation
        fixture.Customize<ProxyTarget>(composer => composer
            .With(pt => pt.Name, () => $"Target_{fixture.Create<int>()}")
            .With(pt => pt.BaseUrl, () => $"https://example.com/{Guid.NewGuid()}") // Valid URL
            .With(pt => pt.IsActive, true)
            .With(pt => pt.TimeoutSeconds, 30)
            .With(pt => pt.CreatedAt, DateTime.UtcNow)
            .With(pt => pt.UpdatedAt, (DateTime?)null)
        );

        // Configure Customer entity generation
        fixture.Customize<Customer>(composer => composer
            .With(c => c.FirstName, () => $"FirstName_{fixture.Create<int>()}")
            .With(c => c.LastName, () => $"LastName_{fixture.Create<int>()}")
        );

        // Configure ApprovalRequest entity generation
        fixture.Customize<ApprovalRequest>(composer => composer
            .With(a => a.OrderId, () => Guid.NewGuid().ToString())
            .With(a => a.OrderName, () => $"Order_{fixture.Create<int>()}")
            .With(a => a.TotalCost, () => (double)(fixture.Create<int>() % 10000 + 1))
            .With(a => a.Quantity, () => fixture.Create<int>() % 100 + 1)
            .With(a => a.Status, ApprovalStatus.Pending)
            .With(a => a.CreatedAt, DateTime.UtcNow)
            .With(a => a.ProcessedAt, (DateTime?)null)
            .With(a => a.ProcessedBy, (string?)null)
            .With(a => a.Comments, (string?)null)
            .With(a => a.ExpiresAt, () => DateTime.UtcNow.AddHours(24))
        );

        // Configure QueueStatus entity generation
        fixture.Customize<QueueStatus>(composer => composer
            .With(q => q.MessageCount, () => (long)(fixture.Create<int>() % 1000))
        );

        // Note: Forecast has a private constructor and must be created using Forecast.New() factory method
        // Cannot be customized with AutoFixture - use Forecast.New() directly in tests
    }
}

/// <summary>
/// AutoFixture customization that creates valid domain entities with all required fields.
/// Use this when you need fully populated, valid entities for integration tests.
/// </summary>
public class ValidDomainCustomization : DomainCustomization
{
    public new void Customize(IFixture fixture)
    {
        base.Customize(fixture);

        // Additional customizations for creating fully valid entities
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}

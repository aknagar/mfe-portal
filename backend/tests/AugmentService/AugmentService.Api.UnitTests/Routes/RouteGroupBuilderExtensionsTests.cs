using AugmentService.Api.Routes;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AugmentService.Api.UnitTests.Routes;

public class RouteGroupBuilderExtensionsTests
{
    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddOpenApi();
        return builder.Build();
    }

    [Fact]
    public void MapUserGroup_Should_ReturnRouteGroupBuilder()
    {
        var app = BuildApp();
        var group = app.MapUserGroup("/api");
        group.Should().NotBeNull();
    }

    [Fact]
    public void MapAdminGroup_Should_ReturnRouteGroupBuilder()
    {
        var app = BuildApp();
        var group = app.MapAdminGroup("/admin");
        group.Should().NotBeNull();
    }

    [Fact]
    public void MapUserGroup_Should_NotThrow_When_TagNameIsNull()
    {
        var app = BuildApp();
        var act = () => app.MapUserGroup("/api", groupTagName: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void MapUserGroup_Should_NotThrow_When_TagNameIsProvided()
    {
        var app = BuildApp();
        var act = () => app.MapUserGroup("/api", groupTagName: "MyTag");
        act.Should().NotThrow();
    }

    [Fact]
    public void MapAdminGroup_Should_NotThrow_When_TagNameIsNull()
    {
        var app = BuildApp();
        var act = () => app.MapAdminGroup("/admin", groupTagName: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void MapAdminGroup_Should_NotThrow_When_TagNameIsProvided()
    {
        var app = BuildApp();
        var act = () => app.MapAdminGroup("/admin", groupTagName: "AdminTag");
        act.Should().NotThrow();
    }

    [Fact]
    public void MapUserGroup_Should_NotThrow_When_ExtraPoliciesAreProvided()
    {
        var app = BuildApp();
        var act = () => app.MapUserGroup("/api", extraRequiredPolicies: ["Policy1", "Policy2"]);
        act.Should().NotThrow();
    }

    [Fact]
    public void MapAdminGroup_Should_NotThrow_When_ExtraPoliciesAreProvided()
    {
        var app = BuildApp();
        var act = () => app.MapAdminGroup("/admin", extraRequiredPolicies: ["AdminPolicy"]);
        act.Should().NotThrow();
    }

    [Fact]
    public void MapUserGroup_Should_NotThrow_When_ExtraPoliciesIsNull()
    {
        var app = BuildApp();
        var act = () => app.MapUserGroup("/api", extraRequiredPolicies: null);
        act.Should().NotThrow();
    }

    [Fact]
    public void MapAdminGroup_Should_NotThrow_When_ExtraPoliciesIsNull()
    {
        var app = BuildApp();
        var act = () => app.MapAdminGroup("/admin", extraRequiredPolicies: null);
        act.Should().NotThrow();
    }
}

using AugmentService.Api.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Xunit;

namespace AugmentService.Api.UnitTests.Authorization;

public class ApiAuthorizationPolicyTests
{
    [Fact]
    public void AuthorizationPolicy_Admin_Should_BeCorrectValue()
    {
        AugmentService.Api.Authorization.AuthorizationPolicy.Admin.Should().Be("Admin");
    }

    [Fact]
    public void AuthorizationPolicy_User_Should_BeCorrectValue()
    {
        AugmentService.Api.Authorization.AuthorizationPolicy.User.Should().Be("User");
    }

    [Fact]
    public void AuthorizationPolicy_Write_Should_BeCorrectValue()
    {
        AugmentService.Api.Authorization.AuthorizationPolicy.Write.Should().Be("Write");
    }

    [Fact]
    public void Scopes_Write_Should_BeCorrectValue()
    {
        Scopes.Write.Should().Be("write");
    }

    [Fact]
    public void Scopes_Read_Should_BeCorrectValue()
    {
        Scopes.Read.Should().Be("read");
    }

    [Fact]
    public void Scopes_Admin_Should_BeCorrectValue()
    {
        Scopes.Admin.Should().Be("admin");
    }

    [Fact]
    public void AddAuthorizationPolicies_Should_RegisterUserPolicy()
    {
        var options = new AuthorizationOptions();
        options.AddAuthorizationPolicies();
        var policy = options.GetPolicy(AugmentService.Api.Authorization.AuthorizationPolicy.User);
        policy.Should().NotBeNull();
    }

    [Fact]
    public void AddAuthorizationPolicies_Should_RegisterWritePolicy()
    {
        var options = new AuthorizationOptions();
        options.AddAuthorizationPolicies();
        var policy = options.GetPolicy(AugmentService.Api.Authorization.AuthorizationPolicy.Write);
        policy.Should().NotBeNull();
    }

    [Fact]
    public void AddAuthorizationPolicies_Should_RegisterAdminPolicy()
    {
        var options = new AuthorizationOptions();
        options.AddAuthorizationPolicies();
        var policy = options.GetPolicy(AugmentService.Api.Authorization.AuthorizationPolicy.Admin);
        policy.Should().NotBeNull();
    }

    [Fact]
    public void UserPolicy_Should_RequireReadOrAdminScope()
    {
        var options = new AuthorizationOptions();
        options.AddAuthorizationPolicies();
        var policy = options.GetPolicy(AugmentService.Api.Authorization.AuthorizationPolicy.User)!;

        // ClaimRequirements are exposed via Requirements
        var claimReq = policy.Requirements
            .OfType<ClaimsAuthorizationRequirement>()
            .FirstOrDefault(r => r.ClaimType == "scope");

        claimReq.Should().NotBeNull();
        claimReq!.AllowedValues.Should().Contain(Scopes.Read);
        claimReq.AllowedValues.Should().Contain(Scopes.Admin);
    }

    [Fact]
    public void WritePolicy_Should_RequireWriteOrAdminScope()
    {
        var options = new AuthorizationOptions();
        options.AddAuthorizationPolicies();
        var policy = options.GetPolicy(AugmentService.Api.Authorization.AuthorizationPolicy.Write)!;

        var claimReq = policy.Requirements
            .OfType<ClaimsAuthorizationRequirement>()
            .FirstOrDefault(r => r.ClaimType == "scope");

        claimReq.Should().NotBeNull();
        claimReq!.AllowedValues.Should().Contain(Scopes.Write);
        claimReq.AllowedValues.Should().Contain(Scopes.Admin);
    }

    [Fact]
    public void AdminPolicy_Should_RequireOnlyAdminScope()
    {
        var options = new AuthorizationOptions();
        options.AddAuthorizationPolicies();
        var policy = options.GetPolicy(AugmentService.Api.Authorization.AuthorizationPolicy.Admin)!;

        var claimReq = policy.Requirements
            .OfType<ClaimsAuthorizationRequirement>()
            .FirstOrDefault(r => r.ClaimType == "scope");

        claimReq.Should().NotBeNull();
        claimReq!.AllowedValues.Should().ContainSingle(Scopes.Admin);
    }
}

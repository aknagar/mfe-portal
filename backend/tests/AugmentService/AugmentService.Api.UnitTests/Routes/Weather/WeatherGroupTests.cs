using AugmentService.Api.Routes.Weather;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AugmentService.Api.UnitTests.Routes.Weather;

public class WeatherGroupTests
{
    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddOpenApi();
        return builder.Build();
    }

    [Fact]
    public void MapWeatherUserGroup_Should_ReturnWebApplication()
    {
        var app = BuildApp();
        var result = app.MapWeatherUserGroup();
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void MapWeatherAdminGroup_Should_ReturnWebApplication()
    {
        var app = BuildApp();
        var result = app.MapWeatherAdminGroup();
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void MapWeatherUserGroup_Should_NotThrow()
    {
        var app = BuildApp();
        var act = () => app.MapWeatherUserGroup();
        act.Should().NotThrow();
    }

    [Fact]
    public void MapWeatherAdminGroup_Should_NotThrow()
    {
        var app = BuildApp();
        var act = () => app.MapWeatherAdminGroup();
        act.Should().NotThrow();
    }

    [Fact]
    public void MapWeatherUserGroup_And_MapWeatherAdminGroup_Should_BothSucceed()
    {
        var app = BuildApp();
        var act = () =>
        {
            app.MapWeatherUserGroup();
            app.MapWeatherAdminGroup();
        };
        act.Should().NotThrow();
    }
}

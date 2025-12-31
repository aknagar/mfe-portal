using AugmentService.Api.Routes.Weather.Endpoints;
using AugmentService.Api.Routes;

namespace AugmentService.Api.Routes.Weather;

public static class WeatherAdminGroup
{
    public static WebApplication MapWeatherAdminGroup(this WebApplication app)
    {
        var group = app.MapAdminGroup("weather");

        group.MapDelete("/{date}", DeleteWeather.Handle);

        return app;
    }
}


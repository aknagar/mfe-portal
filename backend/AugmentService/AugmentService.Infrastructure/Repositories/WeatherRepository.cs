using Application.Weather;
using AugmentService.Core;
using AugmentService.Core.Interfaces;
using AugmentService.Infrastructure.WeatherData;
using Microsoft.EntityFrameworkCore;

namespace AugmentService.Infrastructure.Repositories;

public class WeatherRepository(WeatherDatabaseContext context) : IWeatherRepository
{
    public async Task<Forecast?> GetForecastAsync(DateOnly date)
    {
        return await context.Forecasts
            .Where(f => !f.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task AddForecastAsync(Forecast forecast)
    {
        await context.Forecasts.AddAsync(forecast);
    }
}

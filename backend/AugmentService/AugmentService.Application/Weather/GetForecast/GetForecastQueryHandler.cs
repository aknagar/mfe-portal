using AugmentService.Core.Interfaces;
using FluentResults;
using MediatR;

namespace Application.Weather.GetForecast;

internal class GetForecastQueryHandler(IWeatherRepository weatherRepository)
    : IRequestHandler<GetForecastQuery, Result<GetForecastQueryResponse>>
{
    public async Task<Result<GetForecastQueryResponse>> Handle(GetForecastQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var forecast = await weatherRepository.GetForecastAsync(request.From);
            if (forecast is null)
                return Result.Fail(new Error("No Forecast exists"));
            return new GetForecastQueryResponse(forecast.Date, forecast.TemperatureC, forecast.Summary);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to get forecast.").CausedBy(ex));
        }
    }
}

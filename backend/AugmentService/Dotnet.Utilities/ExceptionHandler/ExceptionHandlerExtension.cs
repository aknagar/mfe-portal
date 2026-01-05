
using Microsoft.Extensions.Logging;

public static class ExceptionHandlerExtension {

    /*
    // https://www.thecodebuzz.com/best-practices-for-handling-exception-in-net-core-2-1/
    public static WebApplication UseApiExceptionHandler(this WebApplication app, ILoggerFactory loggerFactory)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                //if any exception then report it and log it
                if (contextFeature != null)
                {
                    //Technical Exception for troubleshooting
                    var logger = loggerFactory.CreateLogger("GlobalException");
                    logger.LogError($"UnhandledException: {contextFeature.Error}");

                    //Business exception - exit gracefully
                    await context.Response.WriteAsync(new ErrorDetails()
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = "Something went wrongs.Please try again later"
                    }.ToString());
                }
            });
        });
    }
    */
}
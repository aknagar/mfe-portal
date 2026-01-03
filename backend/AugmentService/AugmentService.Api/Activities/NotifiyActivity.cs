using Dapr.Workflow;

namespace AugmentService.Api.Activities
{
    record Notification(string Message);

    class NotifyActivity : WorkflowActivity<Notification, object>
    {
        readonly ILogger logger;

        public NotifyActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<NotifyActivity>();
        }

        public override Task<object> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            this.logger.LogInformation(notification.Message);

            return Task.FromResult<object>(null!);
        }
    }
}


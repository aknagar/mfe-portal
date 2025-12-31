using Azure.Messaging.ServiceBus;

namespace AugmentService.Api.Routes.Orders
{
    public static class OrderRoute
    {
        public static WebApplication MapNotify(this WebApplication app)
        {
            app.MapPost("/notify", static async (ServiceBusClient client, string message) =>
            {                
                var sender = client.CreateSender("notifications");

                // Create a batch
                using ServiceBusMessageBatch messageBatch =
                    await sender.CreateMessageBatchAsync();

                if (messageBatch.TryAddMessage(
                        new ServiceBusMessage($"Message {message}")) is false)
                {
                    // If it's too large for the batch.
                    throw new Exception(
                        $"The message {message} is too large to fit in the batch.");
                }

                // Use the producer client to send the batch of
                // messages to the Service Bus topic.
                await sender.SendMessagesAsync(messageBatch);

                Console.WriteLine($"A message has been published to the topic.");
            });

            return app;
        }
    }
}


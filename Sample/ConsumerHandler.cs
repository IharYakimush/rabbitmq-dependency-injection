using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.DependencyInjection;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    public class ConsumerHandler : AsyncEventingHandler
    {
        private readonly ILogger<ConsumerHandler> logger;

        public ConsumerHandler(ILogger<ConsumerHandler> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override string QueueName => RabbitMqSetup.Queue1.Name;

        public override bool AutoAck => true;

        public override Task HandleMessageAsync(AsyncEventingBasicConsumer consumer, BasicDeliverEventArgs msg)
        {
            this.logger.LogInformation("Recieved {value}", Encoding.UTF8.GetString(msg.Body.ToArray()));

            return Task.CompletedTask;
        }        
    }
}

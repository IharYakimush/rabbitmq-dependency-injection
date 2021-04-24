using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.DependencyInjection;

namespace Sample
{
    internal class Consumer : IHostedService
    {
        private readonly IRabbitMqModel<RabbitMqSetup.Queue1> queue;
        private readonly ILogger<Consumer> logger;
        private string tag = null;

        public Consumer(IRabbitMqModel<RabbitMqSetup.Queue1> queue, ILogger<Consumer> logger)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(this.queue.Model);
            consumer.Received += this.ConsumerReceived;
            this.tag = this.queue.Model.BasicConsume(RabbitMqSetup.Queue1.Name, true, consumer);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.queue.Model.IsOpen)
            {
                this.queue.Model.BasicCancelNoWait(this.tag);
            }

            return Task.CompletedTask;
        }

        private Task ConsumerReceived(object sender, BasicDeliverEventArgs msg)
        {
            this.logger.LogInformation("Recieved {value}", Encoding.UTF8.GetString(msg.Body.ToArray()));

            return Task.CompletedTask;
        }
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMq.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class Consumer : BackgroundService
    {
        private readonly IRabbitMqModel<RabbitMqSetup.Queue1> queue;
        private readonly ILogger<Consumer> logger;

        public Consumer(IRabbitMqModel<RabbitMqSetup.Queue1> queue, ILogger<Consumer> logger)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(this.queue.Model);
            consumer.Received += ConsumerReceived;
            string tag = this.queue.Model.BasicConsume(RabbitMqSetup.Queue1.Name, true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!this.queue.Model.IsOpen)
                {
                    throw new Exception();
                }

                await Task.Delay(1000);
            }

            this.queue.Model.BasicCancelNoWait(tag);
        }

        private Task ConsumerReceived(object sender, BasicDeliverEventArgs msg)
        {
            this.logger.LogInformation("Recieved {value}", Encoding.UTF8.GetString(msg.Body.ToArray()));

            return Task.CompletedTask;
        }
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.DependencyInjection;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    internal class Producer : BackgroundService
    {
        private readonly RabbitMqModelsObjectPool<RabbitMqSetup.Exc1> excObjectPool;
        private readonly ILogger<Producer> logger;

        public Producer(RabbitMqModelsObjectPool<RabbitMqSetup.Exc1> excObjectPool, ILogger<Producer> logger)
        {
            this.excObjectPool = excObjectPool ?? throw new ArgumentNullException(nameof(excObjectPool));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string value = Guid.NewGuid().ToString();

                IModel model = null;
                try
                {
                    model = excObjectPool.Get();
                    model.BasicPublish(RabbitMqSetup.Exc1.Name, "routingKey", false, null, Encoding.UTF8.GetBytes(value));

                    logger.LogInformation("Published {value}", value);
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, "Exception");
                }
                finally
                {
                    excObjectPool.Return(model);
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

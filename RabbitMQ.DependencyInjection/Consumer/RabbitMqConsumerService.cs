using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.DependencyInjection
{
    internal sealed class RabbitMqConsumerService<TModel, THandler> : IHostedService where THandler : class, IConsumerHandler
    {
        private readonly RabbitMqModelsObjectPool<TModel> models;
        private readonly THandler handler;
        private IModel model = null;
        private string tag = null;

        public RabbitMqConsumerService(RabbitMqModelsObjectPool<TModel> models, THandler handler)
        {
            this.models = models ?? throw new ArgumentNullException(nameof(models));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            model = models.Get();
            tag = handler.BasicConsume(model);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (model != null && model.IsOpen && tag != null)
            {
                model.BasicCancel(tag);
            }

            models.Return(model);
            model = null;
            tag = null;

            return Task.CompletedTask;
        }
    }
}

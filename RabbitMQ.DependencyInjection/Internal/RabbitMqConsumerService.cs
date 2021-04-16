using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using RabbitMQ.Client;

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
            this.model = this.models.Get();
            this.tag = this.handler.BasicConsume(this.model);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.model != null && this.model.IsOpen && this.tag != null)
            {
                this.model.BasicCancel(this.tag);
            }

            this.models.Return(this.model);
            this.model = null;
            this.tag = null;

            return Task.CompletedTask;
        }
    }
}

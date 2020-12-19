using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System;

namespace RabbitMQ.DependencyInjection
{
    internal sealed class ModelPoolPolicy<TModel, TConnection> : IPooledObjectPolicy<IModel>
    {
        private readonly IRabbitMqConnection<TConnection> connectionProvider;
        private readonly Action<IModel> bootstrapAction;
        private readonly ILogger logger = null;

        public ModelPoolPolicy(IRabbitMqConnection<TConnection> connection, ILoggerFactory loggerFactory, Action<IModel> bootstrapAction)
        {
            this.connectionProvider = connection ?? throw new ArgumentNullException(nameof(connection));
            this.bootstrapAction = bootstrapAction ?? throw new ArgumentNullException(nameof(bootstrapAction));

            if (loggerFactory != null)
            {
                this.logger = loggerFactory.CreateLogger(Logging.Model.CategoryName);
            }
        }
        public IModel Create()
        {
            var model = this.connectionProvider.Connection.CreateModel();

            if (this.logger != null && this.logger.IsEnabled(Logging.Model.CreatedEventLevel))
            {
                this.logger.Log(Logging.Model.CreatedEventLevel, Logging.Model.CreatedEventId, "Model of type {TypeParam} created", typeof(TModel));
            }

            try
            {
                this.bootstrapAction.Invoke(model);
            }
            catch (Exception exception)
            {
                if (this.logger != null && this.logger.IsEnabled(Logging.Model.BootstrapErrorEventLevel))
                {
                    this.logger.Log(Logging.Model.BootstrapErrorEventLevel, Logging.Model.BootstrapErrorEventId, exception, "Model of type {TypeParam} bootstrap error", typeof(TModel));
                }

                throw;
            }

            return model;
        }

        public bool Return(IModel obj)
        {
            bool result = obj?.IsOpen ?? false;

            if (this.logger != null && this.logger.IsEnabled(Logging.Model.ReturnEventLevel))
            {
                this.logger.Log(Logging.Model.ReturnEventLevel, Logging.Model.ReturnEventId, "Model of type {TypeParam} return to ObjectPool {reuse}", typeof(TModel), result);
            }

            return result;
        }
    }
}

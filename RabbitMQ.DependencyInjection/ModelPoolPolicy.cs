using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System;

namespace RabbitMq.DependencyInjection
{
    sealed class ModelPoolPolicy : IPooledObjectPolicy<IModel>
    {
        private readonly IConnection connection;
        private readonly Action<IModel> bootstrapAction;

        public ModelPoolPolicy(IConnection connection, Action<IModel> bootstrapAction)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.bootstrapAction = bootstrapAction ?? throw new ArgumentNullException(nameof(bootstrapAction));
        }
        public IModel Create()
        {
            var model = this.connection.CreateModel();
            this.bootstrapAction.Invoke(model);

            return model;
        }

        public bool Return(IModel obj)
        {
            return obj.IsOpen;
        }
    }
}

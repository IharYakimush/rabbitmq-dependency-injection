using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.DependencyInjection
{
    sealed class RabbitMqModelFor<TTarget> : IRabbitMqModelFor<TTarget>
    {
        private readonly ObjectPool<IModel> models;
        public RabbitMqModelFor(IPooledObjectPolicy<IModel> policy, int maxRetained)
        {
            this.models = new DefaultObjectPool<IModel>(policy, maxRetained);
        }
        public void Execute(Action<IModel> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var model = this.models.Get();
            try
            {
                action(model);
            }
            finally
            {
                this.models.Return(model);
            }
        }
    }
}

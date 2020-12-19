using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.DependencyInjection
{
    public class RabbitMqModelsObjectPool<TModel> : DefaultObjectPool<IModel>
    {
        public RabbitMqModelsObjectPool(IPooledObjectPolicy<IModel> policy, int maximumRetained) : base(policy, maximumRetained)
        {

        }                
    }
}

using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;

namespace RabbitMQ.DependencyInjection
{
    public class RabbitMqModelsObjectPool<TModel> : DefaultObjectPool<IModel>
    {
        public RabbitMqModelsObjectPool(IPooledObjectPolicy<IModel> policy, int maximumRetained) : base(policy, maximumRetained)
        {

        }                
    }
}

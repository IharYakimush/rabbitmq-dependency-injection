using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;

namespace RabbitMQ.DependencyInjection
{
    public class RabbitMqModelsObjectPool<TModel> : DefaultObjectPool<IModel>
    {
        private readonly bool disableObjectPool;
        public RabbitMqModelsObjectPool(IPooledObjectPolicy<IModel> policy, int maximumRetained) : base(policy, maximumRetained <= 0 ? 1 : maximumRetained)
        {
            this.disableObjectPool = maximumRetained <= 0;
        }

        public override void Return(IModel obj)
        {
            if (this.disableObjectPool && obj.IsOpen)
            {
                obj.Dispose();
            }

            base.Return(obj);
        }
    }
}

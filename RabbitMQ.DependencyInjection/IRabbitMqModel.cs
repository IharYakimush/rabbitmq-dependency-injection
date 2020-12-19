using RabbitMQ.Client;

namespace RabbitMQ.DependencyInjection
{
    public interface IRabbitMqModel<TModel>
    {
        IModel Model { get; }
    }
}

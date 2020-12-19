using RabbitMQ.Client;

namespace RabbitMQ.DependencyInjection
{
    public interface IRabbitMqConnection<TConnection>
    {
        IConnection Connection { get; }
    }
}

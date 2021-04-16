using RabbitMQ.Client;

namespace RabbitMQ.DependencyInjection
{
    public interface IConsumerHandler
    {
        string BasicConsume(IModel model);
    }
}

using RabbitMQ.Client.Events;
using RabbitMQ.DependencyInjection;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    public class ConsumerHandler : AsyncEventingHandler
    {
        public override string QueueName => RabbitMqSetup.Queue1.Name;

        public override bool AutoAck => true;

        public override Task HandleMessageAsync(AsyncEventingBasicConsumer consumer, BasicDeliverEventArgs msg)
        {
            Console.WriteLine($"Recieved {Encoding.UTF8.GetString(msg.Body.ToArray())}");

            return Task.CompletedTask;
        }
    }
}

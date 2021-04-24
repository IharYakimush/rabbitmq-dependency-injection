using System;
using System.Text;

using RabbitMQ.Client.Events;
using RabbitMQ.DependencyInjection;

namespace SampleWeb
{
    public class ConsumerHandlerSync : EventingHandler
    {
        public override string QueueName => "myQueue";

        public override bool AutoAck => true;

        protected override void HandleMessage(EventingBasicConsumer consumer, BasicDeliverEventArgs msg)
        {
            Console.WriteLine($"Recieved {Encoding.UTF8.GetString(msg.Body.ToArray())}");
        }
    }
}

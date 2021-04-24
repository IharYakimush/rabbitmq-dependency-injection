using System.Collections.Generic;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.DependencyInjection
{
    public abstract class EventingHandler : IConsumerHandler
    {
        public string BasicConsume(IModel model)
        {
            var consumer = new EventingBasicConsumer(model);
            consumer.Received += this.Received;

            return model.BasicConsume(consumer, this.QueueName, this.AutoAck, this.ConsumerTag, this.NoLocal, this.Exclusive, this.Arguments);
        }

        private void Received(object sender, BasicDeliverEventArgs msg)
        {
            this.HandleMessage((EventingBasicConsumer)sender, msg);
        }

        public abstract void HandleMessage(EventingBasicConsumer consumer, BasicDeliverEventArgs msg);
        public abstract string QueueName { get; }
        public abstract bool AutoAck { get; }
        public virtual bool NoLocal { get; } = false;
        public virtual bool Exclusive { get; } = false;
        public virtual string ConsumerTag { get; } = "";
        public virtual IDictionary<string, object> Arguments { get; } = null;
    }
}

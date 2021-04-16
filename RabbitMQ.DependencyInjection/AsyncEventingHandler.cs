using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.DependencyInjection
{
    public abstract class AsyncEventingHandler : IConsumerHandler
    {
        public string BasicConsume(IModel model)
        {
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(model);
            consumer.Received += Received;

            return model.BasicConsume(consumer, this.QueueName, this.AutoAck, this.ConsumerTag, this.NoLocal, this.Exclusive, this.Arguments);
        }

        private Task Received(object sender, BasicDeliverEventArgs msg)
        {
            return this.HandleMessageAsync((AsyncEventingBasicConsumer)sender, msg);
        }

        public abstract Task HandleMessageAsync(AsyncEventingBasicConsumer consumer, BasicDeliverEventArgs msg);
        public abstract string QueueName { get; }
        public abstract bool AutoAck { get; }
        public virtual bool NoLocal { get; } = false;
        public virtual bool Exclusive { get; } = false;
        public virtual string ConsumerTag { get; } = "";
        public virtual IDictionary<string, object> Arguments { get; } = null;
    }
}

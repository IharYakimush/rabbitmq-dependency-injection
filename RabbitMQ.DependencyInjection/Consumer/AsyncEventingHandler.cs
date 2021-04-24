using System.Collections.Generic;
using System.Threading.Tasks;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.DependencyInjection
{
    public abstract class AsyncEventingHandler : EventingHandler
    {
        public override string BasicConsume(IModel model)
        {
            var consumer = new AsyncEventingBasicConsumer(model);
            consumer.Received += this.Received;

            return model.BasicConsume(consumer, this.QueueName, this.AutoAck, this.ConsumerTag, this.NoLocal, this.Exclusive, this.Arguments);
        }

        private Task Received(object sender, BasicDeliverEventArgs msg)
        {
            return this.HandleMessageAsync((AsyncEventingBasicConsumer)sender, msg);
        }

        protected abstract Task HandleMessageAsync(AsyncEventingBasicConsumer consumer, BasicDeliverEventArgs msg);

        protected sealed override void HandleMessage(EventingBasicConsumer consumer, BasicDeliverEventArgs msg)
        {
            throw new System.NotImplementedException();
        }
    }
}

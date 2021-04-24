using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

namespace RabbitMQ.DependencyInjection
{
    internal sealed class RabbitMqConsumerService<TModel, THandler> : IHostedService where THandler : class, IConsumerHandler
    {
        private readonly RabbitMqModelsObjectPool<TModel> models;
        private readonly THandler handler;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly ILogger logger = null;
        private IModel model = null;
        private string tag = null;
        private bool stopping = false;

        public RabbitMqConsumerService(RabbitMqModelsObjectPool<TModel> models, 
            THandler handler, 
            IHostApplicationLifetime applicationLifetime, 
            ILoggerFactory loggerFactory)
        {
            this.models = models ?? throw new ArgumentNullException(nameof(models));
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));

            if (loggerFactory != null)
            {
                this.logger = loggerFactory.CreateLogger(Logging.ConsumerService.CategoryName);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.logger != null && this.logger.IsEnabled(Logging.ConsumerService.StartingEventLevel))
            {
                if (this.handler is EventingHandler eh)
                {
                    this.logger.Log(Logging.ConsumerService.StartingEventLevel,
                    Logging.ConsumerService.StartingEventId,
                    "Consumer service for handler of type {HandlerTypeParam} and model of type {TypeParam} starting. QueueName {QueueName} AutoAck {AutoAck} ConsumerTag {ConsumerTag} Exclusive {Exclusive} NoLocal {NoLocal}",
                    typeof(THandler),
                    typeof(TModel),
                    eh.QueueName,
                    eh.AutoAck,
                    eh.ConsumerTag,
                    eh.Exclusive,
                    eh.NoLocal);
                }
                else
                {
                    this.logger.Log(Logging.ConsumerService.StartingEventLevel,
                    Logging.ConsumerService.StartingEventId,
                    "Consumer service for handler of type {HandlerTypeParam} and model of type {TypeParam} starting.",
                    typeof(THandler),
                    typeof(TModel));
                }
            }

            this.model = this.models.Get();
            this.model.ModelShutdown += this.ModelShutdown;
            this.tag = this.handler.BasicConsume(this.model);

            if (this.logger != null && this.logger.IsEnabled(Logging.ConsumerService.StartedEventLevel))
            {
                this.logger.Log(Logging.ConsumerService.StartedEventLevel,
                    Logging.ConsumerService.StartedEventId,
                    "Consumer service for handler of type {HandlerTypeParam} and model {ChannelNumber} of type {TypeParam} started. ConsumerTag {ConsumerTag}",
                    typeof(THandler),
                    this.model.ChannelNumber,
                    typeof(TModel),
                    this.tag);
            }

            return Task.CompletedTask;
        }

        private void ModelShutdown(object sender, ShutdownEventArgs e)
        {
            this.model.ModelShutdown -= ModelShutdown;

            if (!this.stopping)
            {
                if (this.logger != null && this.logger.IsEnabled(Logging.ConsumerService.FailureEventLevel))
                {
                    this.logger.Log(Logging.ConsumerService.FailureEventLevel,
                        Logging.ConsumerService.FailureEventId,
                        "Consumer service failure. Application will be stopped. Model {ChannelNumber} of type {TypeParam} shutdown. {Initiator} {ReplyCode} {ReplyText} {Cause}",
                        this.model?.ChannelNumber, typeof(TModel), e.Initiator, e.ReplyCode, e.ReplyText, e.Cause);
                }

                Environment.ExitCode = 1;
                this.applicationLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.logger != null && this.logger.IsEnabled(Logging.ConsumerService.StopingEventLevel))
            {
                this.logger.Log(Logging.ConsumerService.StopingEventLevel,
                    Logging.ConsumerService.StopingEventId,
                    "Consumer service for handler of type {HandlerTypeParam} and model {ChannelNumber} of type {TypeParam} stoping. ConsumerTag {ConsumerTag}",
                    typeof(THandler),
                    this.model.ChannelNumber,
                    typeof(TModel),
                    this.tag);
            }

            this.stopping = true;

            if (this.model != null && this.model.IsOpen && this.tag != null)
            {
                this.model.BasicCancel(this.tag);
            }

            this.models.Return(this.model);
            this.model = null;
            this.tag = null;

            return Task.CompletedTask;
        }
    }
}

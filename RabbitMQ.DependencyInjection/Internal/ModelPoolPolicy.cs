using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.DependencyInjection
{
    internal sealed class ModelPoolPolicy<TModel, TConnection> : IPooledObjectPolicy<IModel>
    {
        private readonly IRabbitMqConnection<TConnection> connectionProvider;
        private readonly Action<IModel> bootstrapAction;
        private readonly ILogger logger = null;

        public ModelPoolPolicy(IRabbitMqConnection<TConnection> connection, ILoggerFactory loggerFactory, Action<IModel> bootstrapAction)
        {
            this.connectionProvider = connection ?? throw new ArgumentNullException(nameof(connection));
            this.bootstrapAction = bootstrapAction ?? throw new ArgumentNullException(nameof(bootstrapAction));

            if (loggerFactory != null)
            {
                this.logger = loggerFactory.CreateLogger(Logging.Model.CategoryName);
            }
        }
        public IModel Create()
        {
            IModel model;

            try
            {
                model = this.connectionProvider.Connection.CreateModel();
            }
            catch (Exception exc)
            {
                this.logger?.Log(Logging.Model.CreateExceptionEventLevel, Logging.Model.CreateExceptionEventId, exc, "Model of type {TypeParam} create exception", typeof(TModel));

                throw;
            }


            if (this.logger != null)
            {
                this.logger.Log(Logging.Model.CreatedEventLevel, Logging.Model.CreatedEventId, "Model {ChannelNumber} of type {TypeParam} created", model.ChannelNumber, typeof(TModel));

                if (this.logger.IsEnabled(Logging.Model.BasicRecoverOkEventLevel))
                {
                    model.BasicRecoverOk += this.ModelBasicRecoverOk;
                }

                if (this.logger.IsEnabled(Logging.Model.ShutdownEventLevel))
                {
                    model.ModelShutdown += this.ModelModelShutdown;
                }

                if (this.logger.IsEnabled(Logging.Model.CallbackExceptionEventLevel))
                {
                    model.CallbackException += this.ModelCallbackException;
                }

                if (this.logger.IsEnabled(Logging.Model.BasicReturnEventLevel))
                {
                    model.BasicReturn += this.ModelBasicReturn;
                }

                if (this.logger.IsEnabled(Logging.Model.BasicAcksEventLevel))
                {
                    model.BasicAcks += this.ModelBasicAcks;
                }

                if (this.logger.IsEnabled(Logging.Model.BasicNacksEventLevel))
                {
                    model.BasicNacks += this.ModelBasicNacks;
                }
            }

            try
            {
                this.bootstrapAction.Invoke(model);
            }
            catch (Exception exception)
            {
                this.logger?.Log(Logging.Model.BootstrapExceptionEventLevel, Logging.Model.BootstrapExceptionEventId, exception, "Model {ChannelNumber} of type {TypeParam} bootstrap error", model.ChannelNumber, typeof(TModel));

                // dispose model because it not going to be returned to pool
                model.Dispose();

                throw;
            }

            return model;
        }

        private void ModelBasicNacks(object sender, BasicNackEventArgs e)
        {
            var model = sender as IModel;
            this.logger.Log(
                Logging.Model.BasicNacksEventLevel,
                Logging.Model.BasicNacksEventId,
                "Model {ChannelNumber} of type {TypeParam} basic nack. DeliveryTag {DeliveryTag}, Requeue {Requeue}, Multiple {Multiple}",
                model?.ChannelNumber, typeof(TModel), e.DeliveryTag, e.Requeue, e.Multiple);
        }

        private void ModelBasicAcks(object sender, BasicAckEventArgs e)
        {
            var model = sender as IModel;
            this.logger.Log(
                Logging.Model.BasicAcksEventLevel,
                Logging.Model.BasicAcksEventId,
                "Model {ChannelNumber} of type {TypeParam} basic ack. DeliveryTag {DeliveryTag}, Multiple {Multiple}",
                model?.ChannelNumber, typeof(TModel), e.DeliveryTag, e.Multiple);
        }

        private void ModelBasicReturn(object sender, BasicReturnEventArgs e)
        {
            var model = sender as IModel;
            this.logger.Log(
                Logging.Model.BasicReturnEventLevel,
                Logging.Model.BasicReturnEventId,
                "Model {ChannelNumber} of type {TypeParam} basic return {ReplyCode} {ReplyText}",
                model?.ChannelNumber, typeof(TModel), e.ReplyCode, e.ReplyText);

        }

        private void ModelCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            var model = sender as IModel;
            this.logger.Log(
                Logging.Model.CallbackExceptionEventLevel,
                Logging.Model.CallbackExceptionEventId, e.Exception,
                "Model {ChannelNumber} of type {TypeParam} callback exception",
                model?.ChannelNumber, typeof(TModel));
        }

        private void ModelModelShutdown(object sender, ShutdownEventArgs e)
        {
            var model = sender as IModel;
            this.logger.Log(
                        Logging.Model.ShutdownEventLevel,
                        Logging.Model.ShutdownEventId,
                        "Model {ChannelNumber} of type {TypeParam} shutdown. {Initiator} {ReplyCode} {ReplyText} {Cause}",
                        model?.ChannelNumber, typeof(TModel), e.Initiator, e.ReplyCode, e.ReplyText, e.Cause);
        }

        private void ModelBasicRecoverOk(object sender, EventArgs e)
        {
            var model = sender as IModel;
            this.logger.Log(
                Logging.Model.BasicRecoverOkEventLevel,
                Logging.Model.BasicRecoverOkEventId,
                "Model {ChannelNumber} of type {TypeParam} recover ok",
                model?.ChannelNumber, typeof(TModel));
        }

        public bool Return(IModel obj)
        {
            return obj?.IsOpen ?? false;
        }
    }
}

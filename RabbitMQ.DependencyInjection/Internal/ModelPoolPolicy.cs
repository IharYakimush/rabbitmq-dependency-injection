using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace RabbitMQ.DependencyInjection
{
    internal sealed class ModelPoolPolicy<TModel, TConnection> : IPooledObjectPolicy<IModel>
    {
        private readonly IRabbitMqConnection<TConnection> connectionProvider;
        private readonly Action<IModel> bootstrapAction;
        private readonly ILogger logger = null;

        public ModelPoolPolicy(IRabbitMqConnection<TConnection> connection, ILoggerFactory loggerFactory, Action<IModel> bootstrapAction)
        {
            connectionProvider = connection ?? throw new ArgumentNullException(nameof(connection));
            this.bootstrapAction = bootstrapAction ?? throw new ArgumentNullException(nameof(bootstrapAction));

            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger(Logging.Model.CategoryName);
            }
        }
        public IModel Create()
        {
            IModel model;

            try
            {
                model = connectionProvider.Connection.CreateModel();
            }
            catch (Exception exc)
            {
                logger?.Log(Logging.Model.CreateExceptionEventLevel, Logging.Model.CreateExceptionEventId, exc, "Model of type {TypeParam} create exception", typeof(TModel));

                throw;
            }


            if (logger != null)
            {
                logger.Log(Logging.Model.CreatedEventLevel, Logging.Model.CreatedEventId, "Model {ChannelNumber} of type {TypeParam} created", model.ChannelNumber, typeof(TModel));

                if (logger.IsEnabled(Logging.Model.BasicRecoverOkEventLevel))
                {
                    model.BasicRecoverOk += ModelBasicRecoverOk;
                }

                if (logger.IsEnabled(Logging.Model.ShutdownEventLevel))
                {
                    model.ModelShutdown += ModelModelShutdown;
                }

                if (logger.IsEnabled(Logging.Model.CallbackExceptionEventLevel))
                {
                    model.CallbackException += ModelCallbackException;
                }

                if (logger.IsEnabled(Logging.Model.BasicReturnEventLevel))
                {
                    model.BasicReturn += ModelBasicReturn;
                }

                if (logger.IsEnabled(Logging.Model.BasicAcksEventLevel))
                {
                    model.BasicAcks += ModelBasicAcks;
                }

                if (logger.IsEnabled(Logging.Model.BasicNacksEventLevel))
                {
                    model.BasicNacks += ModelBasicNacks;
                }
            }

            try
            {
                bootstrapAction.Invoke(model);
            }
            catch (Exception exception)
            {
                logger?.Log(Logging.Model.BootstrapExceptionEventLevel, Logging.Model.BootstrapExceptionEventId, exception, "Model {ChannelNumber} of type {TypeParam} bootstrap error", model.ChannelNumber, typeof(TModel));

                // dispose model because it not going to be returned to pool
                model.Dispose();

                throw;
            }

            return model;
        }

        private void ModelBasicNacks(object sender, BasicNackEventArgs e)
        {
            IModel model = sender as IModel;
            logger.Log(
                Logging.Model.BasicNacksEventLevel,
                Logging.Model.BasicNacksEventId,
                "Model {ChannelNumber} of type {TypeParam} basic nack. DeliveryTag {DeliveryTag}, Requeue {Requeue}, Multiple {Multiple}",
                model?.ChannelNumber, typeof(TModel), e.DeliveryTag, e.Requeue, e.Multiple);
        }

        private void ModelBasicAcks(object sender, BasicAckEventArgs e)
        {
            IModel model = sender as IModel;
            logger.Log(
                Logging.Model.BasicAcksEventLevel,
                Logging.Model.BasicAcksEventId,
                "Model {ChannelNumber} of type {TypeParam} basic ack. DeliveryTag {DeliveryTag}, Multiple {Multiple}",
                model?.ChannelNumber, typeof(TModel), e.DeliveryTag, e.Multiple);
        }

        private void ModelBasicReturn(object sender, BasicReturnEventArgs e)
        {
            IModel model = sender as IModel;
            logger.Log(
                Logging.Model.BasicReturnEventLevel,
                Logging.Model.BasicReturnEventId,
                "Model {ChannelNumber} of type {TypeParam} basic return {ReplyCode} {ReplyText}",
                model?.ChannelNumber, typeof(TModel), e.ReplyCode, e.ReplyText);

        }

        private void ModelCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            IModel model = sender as IModel;
            logger.Log(
                Logging.Model.CallbackExceptionEventLevel,
                Logging.Model.CallbackExceptionEventId, e.Exception,
                "Model {ChannelNumber} of type {TypeParam} callback exception",
                model?.ChannelNumber, typeof(TModel));
        }

        private void ModelModelShutdown(object sender, ShutdownEventArgs e)
        {
            IModel model = sender as IModel;
            logger.Log(
                        Logging.Model.ShutdownEventLevel,
                        Logging.Model.ShutdownEventId,
                        "Model {ChannelNumber} of type {TypeParam} shutdown. {Initiator} {ReplyCode} {ReplyText} {Cause}",
                        model?.ChannelNumber, typeof(TModel), e.Initiator, e.ReplyCode, e.ReplyText, e.Cause);
        }

        private void ModelBasicRecoverOk(object sender, EventArgs e)
        {
            IModel model = sender as IModel;
            logger.Log(
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

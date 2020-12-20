using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
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
                    model.BasicRecoverOk += ModelBasicRecoverOk;
                }

                if (this.logger.IsEnabled(Logging.Model.ShutdownEventLevel))
                {
                    model.ModelShutdown += ModelModelShutdown;
                }

                if (this.logger.IsEnabled(Logging.Model.CallbackExceptionEventLevel))
                {
                    model.CallbackException += ModelCallbackException;
                }
            }

            try
            {
                this.bootstrapAction.Invoke(model);
            }
            catch (Exception exception)
            {
                this.logger?.Log(Logging.Model.BootstrapExceptionEventLevel, Logging.Model.BootstrapExceptionEventId, exception, "Model {ChannelNumber} of type {TypeParam} bootstrap error", model.ChannelNumber, typeof(TModel));

                throw;
            }

            return model;
        }

        private void ModelCallbackException(object sender, Client.Events.CallbackExceptionEventArgs e)
        {
            IModel model = sender as IModel;
            this.logger.Log(Logging.Model.CallbackExceptionEventLevel, Logging.Model.CallbackExceptionEventId, e.Exception, "Model {ChannelNumber} of type {TypeParam} callback exception", model?.ChannelNumber, typeof(TModel));
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
            this.logger.Log(Logging.Model.BasicRecoverOkEventLevel, Logging.Model.BasicRecoverOkEventId, "Model {ChannelNumber} of type {TypeParam} recover ok", model?.ChannelNumber, typeof(TModel));
        }

        public bool Return(IModel obj)
        {            
            return obj?.IsOpen ?? false;
        }
    }
}

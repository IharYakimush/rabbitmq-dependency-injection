using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System;
using System.Linq;

namespace RabbitMQ.DependencyInjection
{
    public class RabbitMqModelsObjectPool<TModel> : ObjectPool<IModel>, IDisposable
    {
        private readonly bool doNotRetain;
        private readonly ILogger logger;
        private readonly ObjectPool<IModel> inner;

        public RabbitMqModelsObjectPool(ILoggerFactory loggerFactory, IPooledObjectPolicy<IModel> policy, int maximumRetained)
        {
            // unable to create disposable object pool using standard approach
            // because it not works with interfaces type params
            Type innerType = typeof(ObjectPool<>).Assembly.DefinedTypes.Single(t => t.Name.StartsWith("DisposableObjectPool"));
            inner = (ObjectPool<IModel>)Activator.CreateInstance(innerType.MakeGenericType(typeof(IModel)), policy, maximumRetained <= 0 ? 1 : maximumRetained);

            // workaround because not possible to create object pool with capacity < 1
            doNotRetain = maximumRetained <= 0;


            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger(Logging.ModelObjectPool.CategoryName);
            }
        }

        public override void Return(IModel obj)
        {
            if (obj == null)
            {
                return;
            }

            if (logger != null)
            {
                LogLevel logLevel = obj.IsOpen
                    ? Logging.ModelObjectPool.ReturningOpenedModelEventLevel
                    : Logging.ModelObjectPool.ReturningClosedModelEventLevel;

                if (logger.IsEnabled(logLevel))
                {
                    EventId eventId = obj.IsOpen
                        ? Logging.ModelObjectPool.ReturningOpenedModelEventId
                        : Logging.ModelObjectPool.ReturningClosedModelEventId;

                    logger.Log(logLevel, eventId, "Model {ChannelNumber} of type {TypeParam} return " + (obj.IsOpen ? "opened" : "closed"), obj.ChannelNumber, typeof(TModel));
                }
            }

            if (doNotRetain)
            {
                obj.Dispose();
            }

            inner.Return(obj);
        }

        public override IModel Get()
        {
            IModel obj = inner.Get();

            if (logger != null)
            {
                LogLevel logLevel = obj.IsOpen
                    ? Logging.ModelObjectPool.GetOpenedModelEventLevel
                    : Logging.ModelObjectPool.GetClosedModelEventLevel;

                if (logger.IsEnabled(logLevel))
                {
                    EventId eventId = obj.IsOpen
                        ? Logging.ModelObjectPool.GetOpenedModelEventId
                        : Logging.ModelObjectPool.GetClosedModelEventId;

                    logger.Log(logLevel, eventId, "Model {ChannelNumber} of type {TypeParam} get " + (obj.IsOpen ? "opened" : "closed"), obj.ChannelNumber, typeof(TModel));
                }
            }

            return obj;
        }

        public void Dispose()
        {
            (inner as IDisposable)?.Dispose();
        }
    }
}

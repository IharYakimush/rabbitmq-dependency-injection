using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;

namespace RabbitMQ.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register <see cref="IRabbitMqConnection{TConnection}"/> service in collection. Prerequisite for model registration.
        /// </summary>
        /// <typeparam name="TConnection">Type parameter to distinguish different connections</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
        /// <param name="setupAction">Action to configure connection details (e.g. server address and credentials)</param>
        /// <param name="lifetime">Connection lifetime to control when it will be disposed. Recommended value <see cref="ServiceLifetime.Singleton"/> to keep one connection open during all application lifetime.</param>
        /// <returns>The original <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddRabbitMqConnection<TConnection>(
            this IServiceCollection services,
            Func<IServiceProvider, ConnectionFactory> setupAction,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction is null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            if (services.Any(d => d.ServiceType == typeof(IRabbitMqConnection<TConnection>)))
            {
                throw new InvalidOperationException($"Connection with type param {typeof(TConnection)} already added");
            }

            services.Add(new ServiceDescriptor(
                typeof(IRabbitMqConnection<TConnection>), sp =>
                 {
                     ILogger logger = sp.GetService<ILoggerFactory>()?.CreateLogger(Logging.Connection.CategoryName);

                     ConnectionFactory factory;
                     IConnection result;
                     try
                     {
                         factory = setupAction.Invoke(sp);
                     }
                     catch (Exception exception)
                     {
                         logger?.Log(
                             Logging.Connection.FactoryExceptionEventLevel,
                             Logging.Connection.FactoryExceptionEventId, exception,
                             "Connection of type {TypeParam} factory setup exception", typeof(TConnection));

                         throw;
                     }

                     try
                     {
                         result = factory.CreateConnection();
                     }
                     catch (Exception exception)
                     {
                         logger?.Log(
                             Logging.Connection.CreateExceptionEventLevel,
                             Logging.Connection.CreateExceptionEventId, exception,
                             "Connection of type {TypeParam} create exception", typeof(TConnection));

                         throw;
                     }

                     if (logger != null)
                     {
                         logger.Log(Logging.Connection.CreatedEventLevel, Logging.Connection.CreatedEventId, "Connection {ClientProvidedName} of type {TypeParam} created", factory.ClientProvidedName, typeof(TConnection));

                         SubscribeConnectionEventsForLogging<TConnection>(result, logger, factory);
                     }

                     return new RabbitMqConnection<TConnection>(result);
                 },
                lifetime));

            return services;
        }

        /// <summary>
        /// Register classes to get <see cref="IModel"/>. 
        /// <para><see cref="RabbitMqModelsObjectPool{TModel}"/> - ObjectPool that can be used to get and return <see cref="IModel"/> instance. It is created with same <see cref="ServiceLifetime"/> as connection. If model returned to ObjectPool in open state and <paramref name="modelsPoolMaxRetained"/> not exceeded then <see cref="IModel"/> instance will be reused.</para>
        /// <para><see cref="IRabbitMqModel{TModel}"/> - It is registered in container with Transient lifetime and when needed created from same ObjectPool <see cref="RabbitMqModelsObjectPool{TModel}"/>. Don't dispose model in your code to allow it returning to object pool automatically.</para>
        /// </summary>
        /// <typeparam name="TModel">Type parameter to distinguish different models</typeparam>
        /// <typeparam name="TConnection">Type parameter to define which connection previously registered by <see cref="AddRabbitMqConnection{TConnection}"/> associated with model</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
        /// <param name="modelsPoolMaxRetained">Models ObjectPool <see cref="RabbitMqModelsObjectPool{TModel}"/> maximum retained items count. Set this value to be >=1 in case of multithreading scenarios to improve model reuse where appropriate. If set to 0 <see cref="RabbitMqModelsObjectPool{TModel}"/> keep available for injection, but will dispose model on each return call to disable reuse.</param>
        /// <param name="modelBootstrapAction">Model bootstrap action that will be executed after new model creation. Usefull for declaring exchanges, queues, etc.</param>        
        /// <returns>The original <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddRabbitMqModel<TModel, TConnection>(
            this IServiceCollection services,
            int modelsPoolMaxRetained,
            Action<IServiceProvider, IModel> modelBootstrapAction)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (modelBootstrapAction is null)
            {
                throw new ArgumentNullException(nameof(modelBootstrapAction));
            }

            ServiceDescriptor connectionDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRabbitMqConnection<TConnection>));

            if (connectionDescriptor == null)
            {
                throw new InvalidOperationException($"Connection with type param {typeof(TConnection)} should be registered first");
            }

            if (services.Any(d => d.ServiceType == typeof(IRabbitMqModel<TModel>)))
            {
                throw new InvalidOperationException($"Model with type param {typeof(TModel)} already added");
            }

            services.Add(new ServiceDescriptor(typeof(RabbitMqModelsObjectPool<TModel>), sp =>
            {
                ModelPoolPolicy<TModel, TConnection> policy = new ModelPoolPolicy<TModel, TConnection>(
                    sp.GetRequiredService<IRabbitMqConnection<TConnection>>(),
                    sp.GetService<ILoggerFactory>(),
                    (m) => modelBootstrapAction(sp, m));

                return new RabbitMqModelsObjectPool<TModel>(sp.GetService<ILoggerFactory>(), policy, modelsPoolMaxRetained);
            }, connectionDescriptor.Lifetime));

            services.AddTransient<IRabbitMqModel<TModel>, RabbitMqModel<TModel>>();

            return services;
        }

        /// <summary>
        /// Register hosted service which will subscribe for queue and manage message processing
        /// </summary>
        /// <typeparam name="TModel">Type param to reference previously registered model where queue should be declared.</typeparam>
        /// <typeparam name="THandler">Type param to reference handler derived from <see cref="IConsumerHandler"/> which going to process messages</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
        /// <returns>The original <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddRabbitMqConsumerHostingService<TModel, THandler>(
            this IServiceCollection services) where THandler : class, IConsumerHandler
        {
            services.TryAddTransient<THandler>();

            ServiceDescriptor modelDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(RabbitMqModelsObjectPool<TModel>));

            if (modelDescriptor == null)
            {
                throw new InvalidOperationException($"Model with type param {typeof(TModel)} should be registered first");
            }

            ServiceDescriptor serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(RabbitMqConsumerService<RabbitMqModelsObjectPool<TModel>, THandler>));

            if (serviceDescriptor != null)
            {
                throw new InvalidOperationException($"Consumer with model type param {typeof(TModel)} and handler type param {typeof(THandler)} already added");
            }

            services.AddHostedService<RabbitMqConsumerService<TModel, THandler>>();

            return services;
        }

        private static void SubscribeConnectionEventsForLogging<TConnection>(IConnection result, ILogger logger, IConnectionFactory factory)
        {
            if (logger.IsEnabled(Logging.Connection.BlockedEventLevel))
            {
                result.ConnectionBlocked += (object sender, ConnectionBlockedEventArgs e) =>
                {
                    logger.Log(
                        Logging.Connection.BlockedEventLevel,
                        Logging.Connection.BlockedEventId,
                        "Connection {ClientProvidedName} of type {TypeParam} blocked. {Reason}", factory.ClientProvidedName, typeof(TConnection), e.Reason);
                };
            }

            if (logger.IsEnabled(Logging.Connection.UnblockedEventLevel))
            {
                result.ConnectionUnblocked += (object sender, EventArgs e) =>
                {
                    logger.Log(
                        Logging.Connection.UnblockedEventLevel,
                        Logging.Connection.UnblockedEventId,
                        "Connection {ClientProvidedName} of type {TypeParam} unblocked.", factory.ClientProvidedName, typeof(TConnection));
                };
            }

            if (logger.IsEnabled(Logging.Connection.ShutdownEventLevel))
            {
                result.ConnectionShutdown += (object sender, ShutdownEventArgs e) =>
                {
                    logger.Log(
                        Logging.Connection.ShutdownEventLevel,
                        Logging.Connection.ShutdownEventId,
                        "Connection {ClientProvidedName} of type {TypeParam} shutdown. {Initiator} {ReplyCode} {ReplyText} {Cause}", factory.ClientProvidedName, typeof(TConnection), e.Initiator, e.ReplyCode, e.ReplyText, e.Cause);
                };
            }
        }
    }
}

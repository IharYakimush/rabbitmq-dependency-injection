using Microsoft.Extensions.DependencyInjection;
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
        /// Register <see cref="IRabbitMqConnectionProvider<TConnection>"/> service in collection. 
        /// </summary>
        /// <typeparam name="TConnection">Type parameter to distinguish different connections</typeparam>
        /// <param name="services"></param>
        /// <param name="setupAction">Action to configure connection details (e.g. server address and credentials)</param>
        /// <param name="lifetime">Connection lifetime to contron when it will be disposed. Recommended value <see cref="ServiceLifetime.Singleton"/> to keep one connection open during all application lifetime.</param>
        /// <returns></returns>
        public static IServiceCollection AddRabbitMqConnection<TConnection>(
            this IServiceCollection services, 
            Action<IServiceProvider,ConnectionFactory> setupAction, 
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

            if (services.Any(d=>d.ServiceType == typeof(IRabbitMqConnection<TConnection>)))
            {
                throw new InvalidOperationException($"Connection with type param {typeof(TConnection)} already added");
            }
            
            services.Add(new ServiceDescriptor(
                typeof(IRabbitMqConnection<TConnection>), sp =>
                 {
                     ConnectionFactory factory = new ConnectionFactory();

                     setupAction.Invoke(sp, factory);

                     IConnection result = factory.CreateConnection();

                     var loggerFactory = sp.GetService<ILoggerFactory>();

                     if (loggerFactory != null)
                     {
                         var logger = loggerFactory.CreateLogger(Logging.Connection.CategoryName);

                         logger.Log(Logging.Connection.CreatedEventLevel, Logging.Connection.CreatedEventId, "Connection {ClientProvidedName} of type {TypeParam} created", factory.ClientProvidedName, typeof(TConnection));

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

                     return new RabbitMqConnection<TConnection>(result);
                 },
                lifetime));
            
            return services;
        }

        public static IServiceCollection AddRabbitMqModel<TModel,TConnection>(
            this IServiceCollection services, 
            Action<IServiceProvider, IModel> modelBootstrapAction,
            int modelsPoolMaxRetained = 1)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (modelBootstrapAction is null)
            {
                throw new ArgumentNullException(nameof(modelBootstrapAction));
            }

            var connectionDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRabbitMqConnection<TConnection>));

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
                var policy = new ModelPoolPolicy<TModel, TConnection>(
                    sp.GetRequiredService<IRabbitMqConnection<TConnection>>(), 
                    sp.GetService<ILoggerFactory>(),
                    (m) => modelBootstrapAction(sp, m));

                return new RabbitMqModelsObjectPool<TModel>(policy, modelsPoolMaxRetained);
            }, connectionDescriptor.Lifetime));

            services.AddTransient<IRabbitMqModel<TModel>, RabbitMqModel<TModel>>();

            return services;
        }       
    }
}

using Microsoft.Extensions.DependencyInjection;
using RabbitMq.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RabbitMQ.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static string DefaultConnectionName { get; set; } = $"{AppDomain.CurrentDomain.FriendlyName}_{Environment.MachineName}";
        private static Dictionary<string, Lazy<IConnection>> connections;
        private static string ConnectionNameFor<TConnection>()
        {
            return typeof(TConnection).FullName;
        }

        public static IServiceCollection AddRabbitMqConnection<TConnection>(this IServiceCollection services, Action<ConnectionFactory> setupAction)
        {
            return services.AddRabbitMqConnection(ConnectionNameFor<TConnection>(), setupAction);
        }

        public static IServiceCollection AddRabbitMqConnection(this IServiceCollection services, Action<ConnectionFactory> setupAction)
        {
            return services.AddRabbitMqConnection(DefaultConnectionName, setupAction);
        }

        public static IServiceCollection AddRabbitMqConnection(this IServiceCollection services, string name, Action<ConnectionFactory> setupAction)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (setupAction is null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            if (connections.ContainsKey(name))
            {
                throw new InvalidOperationException($"Connection with name {name} already added");
            }

            connections.Add(name, new Lazy<IConnection>(() =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                setupAction.Invoke(factory);

                return factory.CreateConnection(name);
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication));

            if (!services.Any(d=> d.ServiceType == typeof(ConnectionsDispose)))
            {
                services.AddSingleton(new ConnectionsDispose(connections));
            }

            return services;
        }

        public static IServiceCollection AddRabbitMqModelFor<TTarger>(
            this IServiceCollection services,
            string connectionName,
            Action<IModel> modelBootstrapAction,
            int modelsPoolMaxRetained = 1)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (connectionName is null)
            {
                throw new ArgumentNullException(nameof(connectionName));
            }

            if (modelBootstrapAction is null)
            {
                throw new ArgumentNullException(nameof(modelBootstrapAction));
            }

            services.AddSingleton<IRabbitMqModelFor<TTarger>>(sp =>
            {
                if (!connections.ContainsKey(connectionName))
                {
                    throw new InvalidOperationException($"Connection with name {connectionName} not registered. Call 'AddRabbitMqConnection' method first");
                }

                ModelPoolPolicy policy = new ModelPoolPolicy(connections[connectionName].Value, modelBootstrapAction);

                return new RabbitMqModelFor<TTarger>(policy, modelsPoolMaxRetained);
            });

            return services;
        }

        public static IServiceCollection AddRabbitMqModelFor<TTarger>(
            this IServiceCollection services,
            Action<IModel> modelBootstrapAction,
            int modelsPoolMaxRetained = 1)
        {
            return services.AddRabbitMqModelFor<TTarger>(DefaultConnectionName, modelBootstrapAction, modelsPoolMaxRetained);
        }

        public static IServiceCollection AddRabbitMqModelFor<TTarger, TConnection>(
            this IServiceCollection services,
            Action<IModel> modelBootstrapAction,
            int modelsPoolMaxRetained = 1)
        {
            return services.AddRabbitMqModelFor<TTarger>(ConnectionNameFor<TConnection>(), modelBootstrapAction, modelsPoolMaxRetained);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.DependencyInjection;

namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new HostBuilder()
                .ConfigureLogging((ILoggingBuilder b) =>
                {
                    b.AddConsole();
                    b.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices(services =>
                {
                    services.AddRabbitMqConnection<RabbitMqSetup.Connection1>(
                        (s) => new ConnectionFactory
                        {
                            ClientProvidedName = "sample1",
                            Endpoint = new AmqpTcpEndpoint("localhost", 5672),
                            UserName = "myUser",
                            Password = "myPass",
                            DispatchConsumersAsync = true
                        }
                    );

                    services.AddRabbitMqModel<RabbitMqSetup.Exc1, RabbitMqSetup.Connection1>(1,(s, m) =>
                    {
                        m.ExchangeDeclare(RabbitMqSetup.Exc1.Name, ExchangeType.Topic, false, true, null);
                    });

                    services.AddRabbitMqModel<RabbitMqSetup.Queue1, RabbitMqSetup.Connection1>(0,(s, m) =>
                    {
                        m.QueueDeclare(RabbitMqSetup.Queue1.Name);
                        m.QueueBind(RabbitMqSetup.Queue1.Name, RabbitMqSetup.Exc1.Name, "#");
                    });

                    services.AddHostedService<Producer>();
                    services.AddHostedService<Consumer>();

                }).Build().Run();
        }
    }
}

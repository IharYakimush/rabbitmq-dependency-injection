using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            new HostBuilder()
                .ConfigureLogging((ILoggingBuilder b) =>
                {
                    b.AddConsole();
                    b.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices(services =>
                {
                    services.AddRabbitMqConnection<RabbitMqSetup.Connection1>((s, f) =>
                    {
                        f.ClientProvidedName = "sample1";
                        f.Endpoint = new AmqpTcpEndpoint("localhost", 5672);
                        f.UserName = "myUser";
                        f.Password = "myPass";
                        f.DispatchConsumersAsync = true;
                    });

                    services.AddRabbitMqModel<RabbitMqSetup.Exc1, RabbitMqSetup.Connection1>((s, m) =>
                    {
                        m.ExchangeDeclare(RabbitMqSetup.Exc1.Name, ExchangeType.Topic, false, true, null);
                    });

                    services.AddRabbitMqModel<RabbitMqSetup.Queue1, RabbitMqSetup.Connection1>((s, m) =>
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

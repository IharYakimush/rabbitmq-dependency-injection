
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RabbitMQ.Client;
using RabbitMQ.DependencyInjection;

namespace SampleWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRabbitMqConnection<Program>(
                        (s) => new ConnectionFactory
                        {
                            ClientProvidedName = "sample1",
                            Endpoint = new AmqpTcpEndpoint("localhost", 5672),
                            UserName = "myUser",
                            Password = "myPass",
                            DispatchConsumersAsync = false,
                            AutomaticRecoveryEnabled = false
                        }
                    );

            services.AddRabbitMqModel<WeatherForecast, Program>(1, (s, m) =>
            {
                m.ExchangeDeclare("myExc", ExchangeType.Topic, false, true, null);
                m.QueueDeclare("myQueue");
                m.QueueBind("myQueue", "myExc", "#");
            });
           
            services.AddRabbitMqConsumerHostingService<WeatherForecast, ConsumerHandlerSync>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

# RabbitMQ.DependencyInjection

### 1. Register one or more RabbitMq connections. 
Use type params to distinguish different instances. By default connection has recommended lifetime Singleton, but it can be configured differently.
### 2. Register one or more RabbitMq models. 
Model registration required 2 type params. First to be able to inject different models to your classes. Second to bind model to connection. Define bootstrap action that will be executed after new model creation. Usefull for declaring exchanges, queues, etc.

```
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
        services.AddRabbitMqConsumerHostingService<RabbitMqSetup.Queue1, ConsumerHandler>();

    }).Build().Run();
}
```

```
public static class RabbitMqSetup
{
    public class Exc1
    {
        public const string Name = "myExc";
    }

    public class Connection1
    {
    }

    public class Queue1
    {
        public const string Name = "myQueue";
    }
}
```
### 3. First option of model usage
Inject `RabbitMqModelsObjectPool<TModel>` class. It is an ObjectPool that can be used to get and return IModel instance. It is created with same service lifetime as connection. If model returned to ObjectPool in open state and "modelsPoolMaxRetained" not exceeded IModel instance will be reused within same type param.
Sample of message producer with this approach:
```
class Producer : BackgroundService
{
    private readonly RabbitMqModelsObjectPool<RabbitMqSetup.Exc1> excObjectPool;
    private readonly ILogger<Producer> logger;

    public Producer(RabbitMqModelsObjectPool<RabbitMqSetup.Exc1> excObjectPool, ILogger<Producer> logger)
    {
        this.excObjectPool = excObjectPool ?? throw new ArgumentNullException(nameof(excObjectPool));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string value = Guid.NewGuid().ToString();

            IModel model = null;
            try
            {
                model = this.excObjectPool.Get();
                model.BasicPublish(RabbitMqSetup.Exc1.Name, "routingKey", false, null, Encoding.UTF8.GetBytes(value));

                this.logger.LogInformation("Published {value}", value);
            }
            finally
            {
                this.excObjectPool.Return(model);
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}
```
### 4. Second option of model usage
Inject `IRabbitMqModel<TModel>` interface. It is registered in container with Transient lifetime and when needed created from same ObjectPool described in section 3. Don't dispose model in your code to allow it returning to object pool automatically. 
Sample of controller with this approach:
```
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IRabbitMqModel<WeatherForecast> _rabbitMqModel;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IRabbitMqModel<WeatherForecast> rabbitMqModel)
    {
        _logger = logger;
        _rabbitMqModel = rabbitMqModel ?? throw new ArgumentNullException(nameof(rabbitMqModel));
    } 
}
```
### Consumer handler
You can develop only message processing custom logic and host it in background service. 
Implement `IConsumerHandler` interface or one of `AsyncEventingHandler` or `EventingHandler` abstract classes.
```
public class ConsumerHandler : AsyncEventingHandler
{
    public override string QueueName => RabbitMqSetup.Queue1.Name;

    public override bool AutoAck => true;

    protected override Task HandleMessageAsync(AsyncEventingBasicConsumer consumer, BasicDeliverEventArgs msg)
    {
        Console.WriteLine($"Recieved {Encoding.UTF8.GetString(msg.Body.ToArray())}");

        return Task.CompletedTask;
    }
}
```
and register using `services.AddRabbitMqConsumerHostingService<RabbitMqSetup.Queue1, ConsumerHandler>();`

# Logging
If `ILoggerFactory` available in container following events will be logged.
### Default events

| Catgory | Event Name | Log Level | Comments |
|-------- | ---------- | ----------| ---------|
RabbitMQ.Connection | ConnectionCreated | Information | - |
RabbitMQ.Connection | ConnectionBlocked | Warning | https://www.rabbitmq.com/connection-blocked.html |
RabbitMQ.Connection | ConnectionUnblocked | Information | - |
RabbitMQ.Connection | ConnectionShutdown | Information | - |
RabbitMQ.Connection | ConnectionCreateException | Error | - |
RabbitMQ.Connection | ConnectionFactorySetupException | Error | - |
RabbitMQ.Model | ModelCreated | Debug | ObjectPool can't provide previously created instance, so create a new one |
RabbitMQ.Model | BasicRecoverOk | Information | - |
RabbitMQ.Model | ModelBootstrapException | Error | - |
RabbitMQ.Model | ModelShutdown | Debug | - |
RabbitMQ.Model | CallbackException | Warning | - |
RabbitMQ.Model | ModelCreateException | Error | - |
RabbitMQ.Model | ModelBasicReturn | Debug | - |
RabbitMQ.Model | ModelBasicAcks | Debug | - |
RabbitMQ.Model | ModelBasicNacks | Debug | - |
RabbitMQ.ModelsObjectPool | ReturningOpenedModel | Debug | Returing model in opened state to object pool |
RabbitMQ.ModelsObjectPool | ReturningClosedModel | Debug | Returing model in closed state to object pool |
RabbitMQ.ModelsObjectPool | GetOpenedModel | Debug | Obtained model in opened state from object pool |
RabbitMQ.ModelsObjectPool | GetClosedModel | Warning | Obtained model in closed state from object pool |



### Console output sample:
```
info: RabbitMQ.Connection[101]
      Connection sample1 of type Sample.RabbitMqSetup+Connection1 created
dbug: RabbitMQ.Model[201]
      Model 1 of type Sample.RabbitMqSetup+Exc1 created
info: Sample.Producer[0]
      Published 1d15311c-21d5-4186-bc26-b4de45013e1c
dbug: RabbitMQ.Model[201]
      Model 2 of type Sample.RabbitMqSetup+Queue1 created
info: Sample.Producer[0]
      Published b1c4a5b6-25f6-4e05-9425-4db8940d6549
info: Sample.Consumer[0]
      Recieved b1c4a5b6-25f6-4e05-9425-4db8940d6549
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
info: RabbitMQ.Connection[104]
      Connection sample1 of type Sample.RabbitMqSetup+Connection1 shutdown. Application 200 Connection close forced (null)
dbug: RabbitMQ.Model[204]
      Model 1 of type Sample.RabbitMqSetup+Exc1 shutdown. Application 200 Connection close forced (null)
dbug: RabbitMQ.Model[204]
      Model 2 of type Sample.RabbitMqSetup+Queue1 shutdown. Application 200 Connection close forced (null)
```
# NuGet
https://www.nuget.org/packages/RabbitMQ.DependencyInjection
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
Sample of message consumer with this approach:
```
internal class Consumer : IHostedService
{
    private readonly IRabbitMqModel<RabbitMqSetup.Queue1> queue;
    private readonly ILogger<Consumer> logger;
    private string tag = null;

    public Consumer(IRabbitMqModel<RabbitMqSetup.Queue1> queue, ILogger<Consumer> logger)
    {
        this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(this.queue.Model);
        consumer.Received += ConsumerReceived;
        this.tag = this.queue.Model.BasicConsume(RabbitMqSetup.Queue1.Name, true, consumer);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.queue.Model.IsOpen)
        {
            this.queue.Model.BasicCancelNoWait(tag);
        }

        return Task.CompletedTask;
    }

    private Task ConsumerReceived(object sender, BasicDeliverEventArgs msg)
    {
        this.logger.LogInformation("Recieved {value}", Encoding.UTF8.GetString(msg.Body.ToArray()));

        return Task.CompletedTask;
    }
}
```
# Logging
If `ILoggerFactory` available in container following events will be logged. You can change default logging category and events level using `RabbitMQ.DependencyInjection.Logging` class.
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

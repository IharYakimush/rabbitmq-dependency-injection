using Microsoft.Extensions.Logging;

namespace RabbitMQ.DependencyInjection
{
    public static class Logging
    {
        public static class Connection
        {
            public static string CategoryName = "RabbitMQ.Connection";

            public static EventId CreatedEventId = new EventId(101, "ConnectionCreated");
            public static LogLevel CreatedEventLevel = LogLevel.Information;

            public static EventId BlockedEventId = new EventId(102, "ConnectionBlocked");
            public static LogLevel BlockedEventLevel = LogLevel.Warning;

            public static EventId UnblockedEventId = new EventId(103, "ConnectionUnblocked");
            public static LogLevel UnblockedEventLevel = LogLevel.Information;

            public static EventId ShutdownEventId = new EventId(104, "ConnectionShutdown");
            public static LogLevel ShutdownEventLevel = LogLevel.Information;

            public static EventId CreateExceptionEventId = new EventId(105, "ConnectionCreateException");
            public static LogLevel CreateExceptionEventLevel = LogLevel.Error;

            public static EventId FactoryExceptionEventId = new EventId(106, "ConnectionFactorySetupException");
            public static LogLevel FactoryExceptionEventLevel = LogLevel.Error;
        }

        public static class Model
        {
            public static string CategoryName = "RabbitMQ.Model";

            public static EventId CreatedEventId = new EventId(201, "ModelCreated");
            public static LogLevel CreatedEventLevel = LogLevel.Debug;

            public static EventId BasicRecoverOkEventId = new EventId(202, "ModelBasicRecoverOk");
            public static LogLevel BasicRecoverOkEventLevel = LogLevel.Information;

            public static EventId BootstrapExceptionEventId = new EventId(203, "ModelBootstrapException");
            public static LogLevel BootstrapExceptionEventLevel = LogLevel.Error;

            public static EventId ShutdownEventId = new EventId(204, "ModelShutdown");
            public static LogLevel ShutdownEventLevel = LogLevel.Debug;

            public static EventId CallbackExceptionEventId = new EventId(205, "ModelCallbackException");
            public static LogLevel CallbackExceptionEventLevel = LogLevel.Warning;

            public static EventId CreateExceptionEventId = new EventId(206, "ModelCreateException");
            public static LogLevel CreateExceptionEventLevel = LogLevel.Error;
        }
    }
}

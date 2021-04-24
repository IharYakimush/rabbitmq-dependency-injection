using Microsoft.Extensions.Logging;

namespace RabbitMQ.DependencyInjection
{
    public static class Logging
    {
        public static class Connection
        {
            public const string CategoryName = "RabbitMQ.Connection";

            public static EventId CreatedEventId { get; } = new EventId(101, "ConnectionCreated");
            public static LogLevel CreatedEventLevel { get; } = LogLevel.Information;

            public static EventId BlockedEventId { get; } = new EventId(102, "ConnectionBlocked");
            public static LogLevel BlockedEventLevel { get; } = LogLevel.Warning;

            public static EventId UnblockedEventId { get; } = new EventId(103, "ConnectionUnblocked");
            public static LogLevel UnblockedEventLevel { get; } = LogLevel.Information;

            public static EventId ShutdownEventId { get; } = new EventId(104, "ConnectionShutdown");
            public static LogLevel ShutdownEventLevel { get; } = LogLevel.Information;

            public static EventId CreateExceptionEventId { get; } = new EventId(105, "ConnectionCreateException");
            public static LogLevel CreateExceptionEventLevel { get; } = LogLevel.Error;

            public static EventId FactoryExceptionEventId { get; } = new EventId(106, "ConnectionFactorySetupException");
            public static LogLevel FactoryExceptionEventLevel { get; } = LogLevel.Error;
        }

        public static class Model
        {
            public const string CategoryName = "RabbitMQ.Model";

            public static EventId CreatedEventId { get; } = new EventId(201, "ModelCreated");
            public static LogLevel CreatedEventLevel { get; } = LogLevel.Debug;

            public static EventId BasicRecoverOkEventId { get; } = new EventId(202, "ModelBasicRecoverOk");
            public static LogLevel BasicRecoverOkEventLevel { get; } = LogLevel.Information;

            public static EventId BootstrapExceptionEventId { get; } = new EventId(203, "ModelBootstrapException");
            public static LogLevel BootstrapExceptionEventLevel { get; } = LogLevel.Error;

            public static EventId ShutdownEventId { get; } = new EventId(204, "ModelShutdown");
            public static LogLevel ShutdownEventLevel { get; } = LogLevel.Debug;

            public static EventId CallbackExceptionEventId { get; } = new EventId(205, "ModelCallbackException");
            public static LogLevel CallbackExceptionEventLevel { get; } = LogLevel.Warning;

            public static EventId CreateExceptionEventId { get; } = new EventId(206, "ModelCreateException");
            public static LogLevel CreateExceptionEventLevel { get; } = LogLevel.Error;

            public static EventId BasicReturnEventId { get; } = new EventId(207, "ModelBasicReturn");
            public static LogLevel BasicReturnEventLevel { get; } = LogLevel.Debug;

            public static EventId BasicAcksEventId { get; } = new EventId(208, "ModelBasicAcks");
            public static LogLevel BasicAcksEventLevel { get; } = LogLevel.Debug;

            public static EventId BasicNacksEventId { get; } = new EventId(209, "ModelBasicNacks");
            public static LogLevel BasicNacksEventLevel { get; } = LogLevel.Debug;
        }

        public static class ModelObjectPool
        {
            public const string CategoryName = "RabbitMQ.ModelsObjectPool";

            public static EventId ReturningOpenedModelEventId { get; } = new EventId(301, "ReturningOpenedModel");
            public static LogLevel ReturningOpenedModelEventLevel { get; } = LogLevel.Debug;

            public static EventId ReturningClosedModelEventId { get; } = new EventId(302, "ReturningClosedModel");
            public static LogLevel ReturningClosedModelEventLevel { get; } = LogLevel.Debug;

            public static EventId GetOpenedModelEventId { get; } = new EventId(303, "GetOpenedModel");
            public static LogLevel GetOpenedModelEventLevel { get; } = LogLevel.Debug;

            public static EventId GetClosedModelEventId { get; } = new EventId(304, "GetClosedModel");
            public static LogLevel GetClosedModelEventLevel { get; } = LogLevel.Warning;
        }
    }
}

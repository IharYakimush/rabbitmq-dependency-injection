using RabbitMQ.Client;
using System;

namespace RabbitMQ.DependencyInjection
{
    internal sealed class RabbitMqConnection<TConnection> : IRabbitMqConnection<TConnection>, IDisposable
    {
        private bool disposedValue;

        public RabbitMqConnection(IConnection connection)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public IConnection Connection { get; }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.Connection.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

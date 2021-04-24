using System;

using RabbitMQ.Client;

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
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.Connection.Dispose();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

using RabbitMQ.Client;
using System;

namespace RabbitMQ.DependencyInjection
{
    class RabbitMqConnection<TConnection> : IRabbitMqConnection<TConnection>, IDisposable
    {
        private bool disposedValue;

        public RabbitMqConnection(IConnection connection)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));            
        }

        public IConnection Connection { get; }

        protected virtual void Dispose(bool disposing)
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

using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.DependencyInjection
{
    sealed class ConnectionsDispose : IDisposable
    {
        private readonly Dictionary<string, Lazy<IConnection>> connections;
        private bool disposedValue;

        public ConnectionsDispose(Dictionary<string, Lazy<IConnection>> connections)
        {
            this.connections = connections ?? throw new ArgumentNullException(nameof(connections));
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var item in this.connections)
                    {
                        try
                        {
                            if (item.Value.IsValueCreated)
                            {
                                item.Value.Value.Dispose();
                            }
                        }
                        catch
                        {
                            // ignore exceptions
                        }
                    }
                }

                disposedValue = true;
            }
        }


        ~ConnectionsDispose()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

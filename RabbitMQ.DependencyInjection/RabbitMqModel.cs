using RabbitMQ.Client;
using System;

namespace RabbitMq.DependencyInjection
{
    internal sealed class RabbitMqModel<TModel> : IRabbitMqModel<TModel>, IDisposable
    {
        private readonly RabbitMqModelsObjectPool<TModel> models;
        private IModel model = null;
        private bool disposedValue;

        public RabbitMqModel(RabbitMqModelsObjectPool<TModel> models)
        {
            this.models = models ?? throw new ArgumentNullException(nameof(models));
        }
        public IModel Model
        {
            get
            {
                if (disposedValue)
                {
                    throw new ObjectDisposedException(typeof(RabbitMqModel<TModel>).Name);
                }

                return this.model ?? (this.model = this.models.Get());
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.model != null)
                    {
                        this.models.Return(this.model);
                    }
                }

                this.model = null;

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

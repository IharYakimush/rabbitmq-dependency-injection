using RabbitMQ.Client;
using System;

namespace RabbitMQ.DependencyInjection
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

                return model ?? (model = models.Get());
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (model != null)
                    {
                        models.Return(model);
                    }
                }

                model = null;

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

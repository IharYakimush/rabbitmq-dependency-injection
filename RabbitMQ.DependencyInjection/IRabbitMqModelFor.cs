﻿using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.DependencyInjection
{
    public interface IRabbitMqModelFor<TTarget>
    {
        void Execute(Action<IModel> action);
    }
}

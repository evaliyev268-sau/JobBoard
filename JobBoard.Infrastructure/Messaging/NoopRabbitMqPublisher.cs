using JobBoard.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure.Messaging
{
    public class NoopRabbitMqPublisher : IRabbitMqPublisher
    {
        public Task PublishAsync(object payload, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}

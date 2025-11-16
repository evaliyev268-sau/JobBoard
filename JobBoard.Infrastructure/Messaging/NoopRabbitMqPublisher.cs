using JobBoard.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure.Messaging
{
    public class NoopRabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly ILogger<NoopRabbitMqPublisher> _logger;

        public NoopRabbitMqPublisher(ILogger<NoopRabbitMqPublisher> logger)
        {
            _logger= logger;
        }
        public Task PublishAsync(object payload, CancellationToken ct = default)
        {

            _logger.LogInformation("Noop publisher active. Message NOT sent to RabbitMQ.");
            return Task.CompletedTask;
        }
    }
}

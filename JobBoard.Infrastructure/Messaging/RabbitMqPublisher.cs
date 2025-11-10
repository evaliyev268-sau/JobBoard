using JobBoard.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;

namespace JobBoard.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly RabbitMqOptions _opt;
        private readonly IConnection _conn;
        private readonly RabbitMQ.Client.IModel _ch;

        public RabbitMqPublisher(IOptions<RabbitMqOptions> opt)
        {
            _opt = opt.Value;
            var factory = new ConnectionFactory
            {
                HostName = _opt.Host,
                Port = _opt.Port,
                UserName = _opt.Username,
                Password = _opt.Password,
                VirtualHost = _opt.VirtualHost,
                AutomaticRecoveryEnabled = true,
                DispatchConsumersAsync = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(30)
            };
            if (_opt.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    Version = SslProtocols.Tls12,
                    ServerName = _opt.Host
                };
            }
            _conn = factory.CreateConnection();
            _ch = _conn.CreateModel();
            _ch.QueueDeclare(_opt.QueueName, durable: true, exclusive: false, autoDelete: false);
        }

        public void Dispose()
        {
            _ch?.Dispose();
            _conn?.Dispose();

        }

        public Task PublishAsync(object payload, CancellationToken ct = default)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            var props = _ch.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; 

            _ch.BasicPublish(exchange: "", routingKey: _opt.QueueName, basicProperties: props, body: body);
            return Task.CompletedTask;
        }
    }
}

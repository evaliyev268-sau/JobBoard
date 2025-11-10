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

namespace JobBoard.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly RabbitMqOptions _opt;
        private readonly IConnection _conn;
        private readonly RabbitMQ.Client.IModel _ch;

        public RabbitMqPublisher(RabbitMqOptions opt)
        {
            _opt = opt;
            var factory = new ConnectionFactory
            {
                HostName = opt.Host,
                Port = opt.Port,
                UserName = opt.Username,
                Password = opt.Password,
                VirtualHost = opt.VirtualHost,
                AutomaticRecoveryEnabled = true,
                DispatchConsumersAsync = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(30)
            };
            if (opt.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    Version = SslProtocols.Tls12,
                    ServerName = opt.Host
                };
            }
            _conn = factory.CreateConnection();
            _ch = _conn.CreateModel();
            _ch.QueueDeclare(opt.QueueName, durable: true, exclusive: false, autoDelete: false);
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

using JobBoard.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace JobBoard.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly RabbitMqOptions _opt;
        private readonly IConnection _conn;
        private readonly IModel _ch;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public RabbitMqPublisher(RabbitMqOptions opt, ILogger<RabbitMqPublisher> logger)
        {
            _opt = opt;
            _logger = logger;


            var factory = new ConnectionFactory { 
                HostName = _opt.Host,
                Port= _opt.Port,
                UserName = _opt.UserName,
                Password= _opt.Password,
                VirtualHost = _opt.VirtualHost,
                AutomaticRecoveryEnabled=true
               
                };


            if (_opt.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName=_opt.Host
                };
            }

            _conn = factory.CreateConnection();
            _ch = _conn.CreateModel();

            _ch.QueueDeclare(
                queue:_opt.QueueName,
                durable:true,
                exclusive:false,
                autoDelete:false,
                arguments:null
                
             
                );

            _logger.LogInformation("RMQ Publisher connected to {Host}:{Port}",_opt.Host,_opt.Port);


        }



        public void Dispose()
        {
            _ch?.Dispose();
            _conn?.Dispose();
        }

        public Task PublishAsync(object payload, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize
                (payload, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _ch.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "applications/json";
            props.MessageId = Guid.NewGuid().ToString("N");


            _ch.BasicPublish(
                exchange:"",
                routingKey:_opt.QueueName,
                basicProperties:props,
                body:body

                
                
                );

            _logger.LogInformation("Message Published. MessageId:{Id}", props.MessageId);


            return Task.CompletedTask;
        }
    }
}

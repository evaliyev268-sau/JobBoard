using JobBoard.Application.Abstractions;
using RabbitMQ.Client;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;



//using RmqConnectionFactory = RabbitMQ.Client.ConnectionFactory;
//using RmqIConnection = RabbitMQ.Client.IConnection;
//using RmqIChannel = RabbitMQ.Client.IChannel;
//using RmqIBasicProperties = RabbitMQ.Client.IBasicProperties;


namespace JobBoard.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly RabbitMqOptions _opt;
        private readonly IConnection _conn;
        private readonly IChannel _ch;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(IOptions<RabbitMqOptions> opt, ILogger<RabbitMqPublisher> logger)
        {
            _opt = opt.Value;
            _logger = logger;
            var factory = new ConnectionFactory
            {
                HostName = _opt.Host,
                Port = _opt.Port,
                UserName = _opt.UserName,
                Password = _opt.Password,
                VirtualHost = _opt.VirtualHost,
                AutomaticRecoveryEnabled = true,

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
            _conn =  factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _ch = _conn.CreateChannelAsync().GetAwaiter().GetResult();
            _ch.QueueDeclareAsync(_opt.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null).GetAwaiter().GetResult();
            _logger.LogInformation("RabbitMqPublisher connexted to {Host}:{Port}",_opt.Host,_opt.Port);
        }

        public void Dispose()
        {
            
            (_ch as IDisposable)?.Dispose();
            (_conn as IDisposable)?.Dispose();


        }

        public async Task PublishAsync(object payload, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var bodyBytes = Encoding.UTF8.GetBytes(json);
            var bodyMemory= new ReadOnlyMemory<byte>(bodyBytes);
            var preview = json.Length > 400 ? json[..400] + "...{truncated}" : json;

            BasicProperties props = new RabbitMQ.Client.BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = RabbitMQ.Client.DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString("N")
            };

            _logger.LogInformation("Publishing message: {Preview}",preview);


            await _ch.BasicPublishAsync
            (exchange: "", routingKey: _opt.QueueName, mandatory: false, basicProperties:props, body: bodyMemory ).ConfigureAwait(false);

            _logger.LogInformation("Message published. MessageId={MessageId}",props.MessageId
            );
          
        }
    }
}

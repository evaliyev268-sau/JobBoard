using JobBoard.Application.Events;
using JobBoard.Core.Models;
using JobBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

namespace JobBoard.Infrastructure.Messaging
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly RabbitMqOptions _opt;
        private readonly ILogger<RabbitMqConsumerService> _logger;
        private readonly IServiceProvider _sp;
        private IConnection? _connection;
        private RabbitMQ.Client.IModel? _channel;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public RabbitMqConsumerService(IOptions<RabbitMqOptions> opt,ILogger<RabbitMqConsumerService> logger,IServiceProvider sp)
        {
            _opt = opt.Value;
            _logger = logger;
            _sp = sp;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_opt.Host))
            {
                _logger.LogWarning("RabbitMQ host isn t configured");
                return Task.CompletedTask;
            }

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
                    ServerName = _opt.Host
                };
            }

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue:_opt.QueueName,
                    durable:true,
                    exclusive:false,
                    autoDelete:false,
                    arguments:null
                    
                    
                    );

                _channel.BasicQos(prefetchSize: 0, prefetchCount: _opt.PrefetchCount, global: false);

                _logger.LogInformation("RabbitMqConsumerService connected to {Host}:{Port}",_opt.Host,_opt.Port);

            _logger.LogWarning("=== DEBUG: Channel IsOpen: {IsOpen} ===", _channel?.IsOpen);
            _logger.LogWarning("=== DEBUG: Connection IsOpen: {IsOpen} ===", _connection?.IsOpen);

            return base.StartAsync(cancellationToken);
            }


        



        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _logger.LogWarning("=== DEBUG: ExecuteAsync started, Channel IsOpen: {IsOpen} ===", _channel?.IsOpen);

            if (_channel == null)
            {
                _logger.LogWarning("Channel is null");
                return Task.CompletedTask;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, ea) =>
            {
                _logger.LogInformation("###Message Received###");
                var body = ea.Body.ToArray();
                var json=Encoding.UTF8.GetString(body);
                var preview = json.Length > 500 ? json[..500] + "..." : json;
                _logger.LogInformation("Message:{Body}", preview);


                try
                {
                    var evt = JsonSerializer.Deserialize<JobApplicationCreatedEvent>(json, _jsonOptions);
                    if (evt == null || evt.Type != "JobApplicationCreated")
                    {
                        _logger.LogWarning("Invalid event type.");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    _logger.LogInformation("Processing JobId {JobId}.. .", evt.JobId);

                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                  
                    var duplicate = db.JobApplications.Any(
                        a => a.JobId == evt.JobId &&
                             a.ApplicantEmail == evt.ApplicantEmail);

                    if (duplicate)
                    {
                        _logger.LogInformation("Duplicate detected, skipping.");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                   
                    var app = new JobApplication
                    {
                        JobId = evt.JobId,
                        ApplicantName = evt.ApplicantName,
                        ApplicantEmail = evt.ApplicantEmail,
                        AppliedAt = evt.AppliedAt
                    };

                    db.JobApplications.Add(app);
                    db.SaveChanges();

                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Condumed: JobId={JobId}, Email={Email}", evt.JobId, evt.ApplicantEmail);

                }
                catch (Exception ex) {

                    _logger.LogError(ex, "Error proccessing message");
                    _channel.BasicNack(ea.DeliveryTag, false, true);

                }

            };

            _channel.BasicConsume(
                queue:_opt.QueueName,
                autoAck:false,
                consumer:consumer

                );

            _logger.LogInformation("Consumer subscribed to queue:{Queue}",_opt.QueueName);

            return Task.Delay(Timeout.Infinite, stoppingToken);

        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();

        }





    }
}

using JobBoard.Application.Abstractions;
using JobBoard.Application.Events;
using JobBoard.Core.Models;
using JobBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

namespace JobBoard.Infrastructure.Messaging
{
    /// <summary>
    /// RabbitMQ mesajlarını sürekli dinleyen background service.
    /// IRabbitMqConsumer interface'ini implement ederek DI üzerinden erişilebilir.
    /// </summary>
    public class RabbitMqConsumerService : BackgroundService, IRabbitMqConsumer
    {
        private readonly RabbitMqOptions _opt;
        private readonly IServiceProvider _sp;
        private readonly ILogger<RabbitMqConsumerService> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public bool IsConnected => _connection?.IsOpen ?? false;
        public string QueueName => _opt.QueueName;

        public RabbitMqConsumerService(
            IOptions<RabbitMqOptions> opt,
            IServiceProvider sp,
            ILogger<RabbitMqConsumerService> logger)
        {
            _opt = opt.Value;
            _sp = sp;
            _logger = logger;
        }

        /// <summary>
        /// Uygulama başladığında RabbitMQ'ya bağlanır ve queue'yu declare eder.
        /// </summary>
        public override Task StartAsync(CancellationToken ct)
        {
            // RabbitMQ config yoksa başlatma
            if (string.IsNullOrWhiteSpace(_opt.Host))
            {
                _logger.LogWarning("RabbitMQ host is not configured.  Consumer will not start.");
                return Task.CompletedTask;
            }

            try
            {
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

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Queue declare
                _channel.QueueDeclare(
                    queue: _opt.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // QoS ayarla
                _channel.BasicQos(prefetchSize: 0, prefetchCount: _opt.PrefetchCount, global: false);

                _logger.LogInformation(
                    "RMQ Consumer connected to {Host}:{Port}, Queue:{Queue}",
                    _opt.Host, _opt.Port, _opt.QueueName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect RabbitMQ Consumer");
            }

            return base.StartAsync(ct);
        }

        /// <summary>
        /// Background task olarak sürekli mesaj dinler.
        /// </summary>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogWarning("Channel is null, consumer cannot start.");
                return Task.CompletedTask;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, ea) =>
            {
                _logger.LogInformation("Message received from RabbitMQ");

                try
                {
                    // 1. Mesaj body'sini decode et
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var messageId = ea.BasicProperties?.MessageId ?? "N/A";

                    _logger.LogInformation("Processing MessageId:{Id}, Body:{Body}", messageId, json);

                    // 2. Event'e deserialize et
                    var evt = JsonSerializer.Deserialize<JobApplicationCreatedEvent>(json, _jsonOptions);

                    if (evt == null || evt.Type != "JobApplicationCreated")
                    {
                        _logger.LogWarning("Invalid event type received, skipping.");
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    // 3.  Scoped DbContext kullanarak işle
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Duplicate check
                    var duplicate = db.JobApplications.Any(a =>
                        a.JobId == evt.JobId &&
                        a.ApplicantEmail == evt.ApplicantEmail &&
                        a.AppliedAt == evt.AppliedAt
                    );

                    if (duplicate)
                    {
                        _logger.LogInformation("Duplicate detected, skipping.  JobId:{JobId}", evt.JobId);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    // 4.  Veritabanına kaydet
                    var app = new JobApplication
                    {
                        JobId = evt.JobId,
                        ApplicantName = evt.ApplicantName,
                        ApplicantEmail = evt.ApplicantEmail,
                        AppliedAt = evt.AppliedAt
                    };

                    db.JobApplications.Add(app);
                    db.SaveChanges();

                    _logger.LogInformation("JobApplication saved.  JobId:{JobId}, Email:{Email}",
                        evt.JobId, evt.ApplicantEmail);

                    // 5. ACK gönder
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");

                    // NACK + requeue:false (Dead letter'a gider)
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Consumer'ı başlat
            _channel.BasicConsume(
                queue: _opt.QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("Consumer registered on queue: {Queue}", _opt.QueueName);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Uygulama kapanırken kaynakları temizle.
        /// </summary>
        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
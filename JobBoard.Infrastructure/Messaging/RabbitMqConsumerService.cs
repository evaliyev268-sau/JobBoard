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
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly RabbitMqOptions _opt;
        private readonly IServiceProvider _sp;
        private readonly ILogger<RabbitMqConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);


        public RabbitMqConsumerService(IOptions<RabbitMqOptions> opt, IServiceProvider sp, ILogger<RabbitMqConsumerService> logger)
        {
            _opt = opt.Value;
            _sp = sp;
            _logger = logger;
        }


        public override async Task StartAsync(CancellationToken ct)
        {

            //No Host
            if (string.IsNullOrWhiteSpace(_opt.Host))
            {
                _logger.LogWarning("RabbitMQ host is not configured. RabbitMqConsumerService will not start.");
                return;
            }


            //Create Factory
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

            //Ssl
            if (_opt.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    Version = SslProtocols.Tls12,
                    ServerName = _opt.Host
                };
            }
            //Create Connection & Channel
            _connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
            _channel = await _connection.CreateChannelAsync(cancellationToken:ct).ConfigureAwait(false);


            //Declare Queue
            await _channel.QueueDeclareAsync(_opt.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct).ConfigureAwait(false);

            //Set Qos
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _opt.PrefetchCount, global: false, cancellationToken: ct).ConfigureAwait(false);


            //Connection Log
            _logger.LogInformation("RabbitMqConsumerService started and connected to RabbitMQ at {Host}:{Port}", _opt.Host, _opt.Port);

            await base.StartAsync(ct);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //No channel
            if (_channel == null) return Task.CompletedTask;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender,ea) =>
            {
                var jsonBody=Encoding.UTF8.GetString(ea.Body.ToArray());
                var displayed=jsonBody.Length>500 ? jsonBody.Substring(0,500)+"...{truncated}" : jsonBody;
                _logger.LogInformation("Received message: {Body}", displayed);

                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var evt = JsonSerializer.Deserialize<JobApplicationCreatedEvent>(json, _jsonOptions);

                    if (evt == null || evt.Type != "JobApplicationCreated")
                    {
                        _logger.LogWarning("Received invalid or unknown event.");
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                        return;
                    }

                    _logger.LogInformation("Processing message for JobId {JobId}, ApplicantEmail {Email}...",evt.JobId, evt.ApplicantEmail);

                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var duplicate = await db.JobApplications.AnyAsync(a => a.JobId == evt.JobId && a.ApplicantEmail == evt.ApplicantEmail && a.AppliedAt == evt.AppliedAt, stoppingToken);


                    if (duplicate)
                    {
                        _logger.LogInformation("Duplicate application detected for JobId: {JobId}, ApplicantEmail: {ApplicantEmail}. Ignoring event.", evt.JobId, evt.ApplicantEmail);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
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
                    await db.SaveChangesAsync(stoppingToken);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    _logger.LogInformation("Processed JobApplicationCreated event for JobId: {JobId}, ApplicantEmail: {ApplicantEmail}", evt.JobId, evt.ApplicantEmail);

                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {Message}", ex.Message);
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                }
            };

            _channel.BasicConsumeAsync(_opt.QueueName, autoAck: false, consumer: consumer);

            return Task.CompletedTask;

        }
        
        public override void Dispose()
        {
            (_channel as IDisposable)?.Dispose();
            (_connection as IDisposable)?.Dispose();
            base.Dispose();
        }
    }
}

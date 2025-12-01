using JobBoard.Application.Abstractions;
using JobBoard.Application.DTOs;
using JobBoard.Core.Models;
using JobBoard.Infrastructure.Data;
using JobBoard.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace JobBoard.Infrastructure.Messaging
{
    public class RabbitMqConsumer : IRabbitMqConsumer, IDisposable
    {
        private readonly RabbitMqOptions _opt;
        private readonly IConnection _conn;
        private readonly IModel _ch;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        private readonly AppDbContext _context;
        public RabbitMqConsumer(RabbitMqOptions opt, ILogger<RabbitMqPublisher> logger, AppDbContext context)
        {
            _opt = opt;
            _logger = logger;
            _context= context;

            var factory = new ConnectionFactory
            {
                HostName = _opt.Host,
                Port = _opt.Port,
                UserName = _opt.UserName,
                Password = _opt.Password,
                VirtualHost = _opt.VirtualHost,
                AutomaticRecoveryEnabled = true

            };


            if (_opt.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = _opt.Host
                };
            }

            _conn = factory.CreateConnection();
            _ch = _conn.CreateModel();

            _ch.QueueDeclare(
                queue: _opt.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null


                );

            _logger.LogInformation("RMQ Consumer connected to {Host}:{Port}", _opt.Host, _opt.Port);
        }






        public Task ConsumeAsync(CancellationToken ct = default)
        {


            
            var consumer = new EventingBasicConsumer(_ch);



            if (consumer!=null)
            {
                _logger.LogError("We are in the sender!1");
            }
            else
            {
                _logger.LogError("We are in the sender!2");
            }


            _ch.BasicConsume(
                queue: _opt.QueueName,
                exclusive: false, 
                consumer: consumer
                );

                consumer.Received += (sender, ea) =>
                {
                    _logger.LogError("We are in the sender!3");

                    try
                    {
                        // 1. Body → JSON string
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                        // 2. Header info
                        var messageId = ea.BasicProperties?.MessageId ?? "N/A";

                        _logger.LogInformation("Message Received. MessageId:{Id}", messageId);

                        // 3. JSON → object (GENERIC DEĞİL, RAW işlem)
                        var payload = JsonSerializer.Deserialize<object>(
                            json,
                            _jsonOptions
                        );


                        var entity = new JobBoard.Core.Models.Job { Id = 41, Title = json, Description = "Oversee project planning and execution." };

                        _context.Jobs.Add(entity);
                        _context.SaveChanges();


                        _logger.LogInformation("Processing Payload: {Data}", payload);



                        // 5. Başarılı → ACK
                        _ch.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        // Hata Log
                        _logger.LogError(ex, "Message Processing Failed!");

                        // BAŞARISIZ → NACK + requeue:false
                        _ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

            _ch.BasicConsume(
                queue: _opt.QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("Consumer Registered. Queue:{Queue}", _opt.QueueName);

            return Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(1000, ct);
                }
            }, ct);
        }

        public void Dispose()
        {
            _ch?.Dispose();
            _conn?.Dispose();
        }
    }
}
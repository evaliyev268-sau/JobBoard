using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure.Messaging
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly RabbitMqOptions _opt;
        private readonly IServiceProvider _sp;
        private readonly ILogger<RabbitMqConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private  readonly JsonSerializerOptions _jsonOptions=new(JsonSerializerDefaults.Web);


        public RabbitMqConsumerService(IOptions<RabbitMqOptions> opt,IServiceProvider sp, ILogger<RabbitMqConsumerService> logger )
        {
            _opt= opt.Value;
            _sp= sp;
            _logger= logger;
        }










        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}

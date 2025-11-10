using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure.Messaging
{
   public class RabbitMqOptions
    {
        public string Host { get; set; } = String.Empty;
        public int Port { get; set; } = 5671;
        public string Username { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public string VirtualHost { get; set; } = "/";

        public bool UseSsl { get; set; } = true;
        public string QueueName { get; set; } = "job_applications";
        public ushort PrefetchCount { get; set; } = 10;
    }
}

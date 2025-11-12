using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure.Messaging
{
   public class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly RabbitMqOptions _opt;
        private readonly IWebHostEnvironment _env;

        public RabbitMqHealthCheck(RabbitMqOptions opt, IWebHostEnvironment env)
        {
            _opt = opt;
            _env = env;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            if(string.IsNullOrEmpty(_opt.Host))
            {
                return Task.FromResult(HealthCheckResult.Degraded("RabbitMQ host not cofigured."));
            }
            bool credsOk = !string.IsNullOrWhiteSpace(_opt.UserName) && !string.IsNullOrWhiteSpace(_opt.Password);

            if (!credsOk)
            {
                if (_env.IsDevelopment())
                {
                  
                    return Task.FromResult(HealthCheckResult.Degraded("RabbitMQ credentials missing in merged configuration (check user-secrets)."));
                }
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ credentials incomplete."));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"RabbitMQ is configured. Host:{_opt.Host}"));
        }
    }
}

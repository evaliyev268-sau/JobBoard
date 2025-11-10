using JobBoard.Application.Abstractions;
using JobBoard.Infrastructure.Data;
using JobBoard.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure
{
    public static class DependecyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration config)
        {
            var cs = config.GetConnectionString("Default") ?? "DataSource=jobboard.db";
            services.AddDbContext<AppDbContext>(opt=>opt.UseSqlite(cs));

            var rmq=new RabbitMqOptions();
            config.GetSection("RabbitMq").Bind(rmq);
            if(!string.IsNullOrEmpty(rmq.Host))
            {
                services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
                services.AddSingleton(rmq);

            }
            else
            {
                services.AddSingleton<IRabbitMqPublisher,RabbitMqPublisher>();
            }
            
            return services;
        }
    }
}

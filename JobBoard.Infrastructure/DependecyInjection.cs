using JobBoard.Application.Abstractions;
using JobBoard.Infrastructure.Data;
using JobBoard.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobBoard.Infrastructure
{
    public static class DependecyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration config)
        {
            var cs = config.GetConnectionString("Default") ?? "DataSource=jobboard.db";
            services.AddDbContext<AppDbContext>(opt=>opt.UseSqlite(cs));

            services.AddScoped<IJobRepository,Repositories.EfJobRepository>();

            services.Configure<RabbitMqOptions>(config.GetSection("RabbitMq"));

            services.AddSingleton<IRabbitMqPublisher>(sp => {
                var opt = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var host = opt.Host?.Trim();
                if(string.IsNullOrWhiteSpace(host))
                {
                    return new NoopRabbitMqPublisher();
                }

                return new RabbitMqPublisher(Options.Create(opt));
            });


            return services;
        }
    }
}

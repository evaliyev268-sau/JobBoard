using JobBoard.Application;
using JobBoard.Infrastructure;
using JobBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HealthChecks.RabbitMQ;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using JobBoard.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);


Log.Logger=new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
  .AddCheck<RabbitMqHealthCheck>("rabbitmq_config");

var app=builder.Build();

using(var scope=app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if(!await db.Jobs.AnyAsync())
    {
        db.Jobs.AddRange(new[]
        {
            new JobBoard.Core.Models.Job{Title="Software Engineer",Description="Develop and maintain software applications."},
            new JobBoard.Core.Models.Job{Title="Data Analyst",Description="Analyze data to help make informed business decisions."},
            new JobBoard.Core.Models.Job{Title="Project Manager",Description="Oversee project planning and execution."}
        });
        await db.SaveChangesAsync();
        Log.Information("Seeded initial job data.");
    }
}
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
   
    
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.MapHealthChecks("/health");


app.Lifetime.ApplicationStarted.Register(()=>
{
    Log.Information("App started in {Environment}", app.Environment.EnvironmentName);
});



app.Run();

Log.CloseAndFlush();
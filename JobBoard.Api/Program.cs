using JobBoard.Application;
using JobBoard.Infrastructure;
using JobBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
    }
}
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
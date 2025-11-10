using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("JOBBOARD_CONN") ?? "Data Source=jobboard.db";

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(cs).Options;

            return new AppDbContext(options);
        }
    }
}

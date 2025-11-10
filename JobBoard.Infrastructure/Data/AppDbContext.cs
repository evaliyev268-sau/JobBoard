using JobBoard.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Infrastructure.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options) {}

        public DbSet<Job> Jobs=>Set<Job>();
        public DbSet<JobApplication> JobApplications=>Set<JobApplication>();


    }
}

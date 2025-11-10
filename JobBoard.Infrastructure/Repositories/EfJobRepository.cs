using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobBoard.Application.Abstractions;
using JobBoard.Core.Models;
using JobBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Repositories
{
    public class EfJobRepository : IJobRepository
    {
        private readonly AppDbContext _db;

        public EfJobRepository(AppDbContext db)
        {
            _db=db;
        }

        public async Task<JobApplication> AddApplicationAsync(JobApplication application, CancellationToken ct = default)
        {
            _db.JobApplications.Add(application);
            await _db.SaveChangesAsync(ct);
            return application;
        }

        public async Task<Job> AddAsync(Job job, CancellationToken ct = default)
        {
            _db.Jobs.Add(job);
            await _db.SaveChangesAsync(ct);
            return job;
        }

        public Task<List<Job>> GetAllAsync(CancellationToken ct = default)
        {
            return _db.Jobs.AsNoTracking().OrderByDescending(x=>x.PostedAt).ToListAsync(ct);
        }

        public Task<Job?> GetByIdAsync(int id, CancellationToken ct = default)
        {
           return _db.Jobs.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct);
        }
    }
}

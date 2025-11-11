using JobBoard.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.Abstractions
{
    public interface IJobRepository
    {
        Task<List<Job>> GetAllAsync(CancellationToken ct = default);
        Task<Job?> GetJobApplicationsByIdAsync(int id, CancellationToken ct = default);
        Task<Job> AddAsync(Job job,CancellationToken ct=default);
        Task<JobApplication> AddApplicationAsync(JobApplication application, CancellationToken ct = default);

        Task<List<JobApplication>> GetApplicationsByJobIdAsync(int jobId, CancellationToken ct = default);
    }
}

using JobBoard.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.Services
{
 public interface IJobService
    {
        Task<List<JobDto>> GetAllAsync(CancellationToken ct=default);
        Task<JobDto?> GetByIdAsync(int id,CancellationToken ct=default);
        Task<JobDto> CreateAsync(CreateJobRequest request, CancellationToken ct = default);
        Task<int> ApplyAsync(int jobId,ApplyRequest request,CancellationToken ct=default);

        Task<List<JobApplicationDto>> GetApplicationAsync(int jobIdm,CancellationToken ct=default);
    }
}

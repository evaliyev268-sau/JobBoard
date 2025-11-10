using JobBoard.Application.Abstractions;
using JobBoard.Application.DTOs;
using JobBoard.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _repo;
        private readonly IRabbitMqPublisher _publisher;
        public JobService(IJobRepository repo,IRabbitMqPublisher publisher)
        {
            _repo = repo;
            _publisher = publisher;
        }
        public async Task<int> ApplyAsync(int jobId, ApplyRequest request, CancellationToken ct = default)
        {
            var app=new JobApplication
            {
                JobId=jobId,
                ApplicantName=request.ApplicantName,
                ApplicantEmail=request.ApplicantEmail,
                
            };

            app=await _repo.AddApplicationAsync(app, ct);

            await _publisher.PublishAsync(new {
                JobId=jobId,
                ApplicantName=request.ApplicantName,
                ApplicantEmail=request.ApplicantEmail,
                AppliedAt=app.AppliedAt
            }, ct);
            return app.Id;

        }

        public async Task<JobDto> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
        {
            var entity = new Job {Title=request.Title,Description=request.Description };
            entity = await _repo.AddAsync(entity, ct);
            return new JobDto(entity.Id, entity.Title, entity.Description);
        }

        public async Task<List<JobDto>> GetAllAsync(CancellationToken ct = default)
        {
            var items = await _repo.GetAllAsync(ct);
            return items.Select(j => new JobDto(j.Id, j.Title, j.Description)).ToList();
        }

        public async Task<JobDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var j = await _repo.GetByIdAsync(id, ct);
            return j is null ? null : new JobDto(j.Id, j.Title, j.Description);

        }
    }
}

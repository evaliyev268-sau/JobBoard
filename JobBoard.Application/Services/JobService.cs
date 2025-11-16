using JobBoard.Application.Abstractions;
using JobBoard.Application.DTOs;
using JobBoard.Application.Events;
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
        private readonly IRealtimeNotifier _notifier;


        public JobService(IJobRepository repo,IRabbitMqPublisher publisher, IRealtimeNotifier notifier)
        {
            _repo = repo;
            _publisher = publisher;
            _notifier = notifier;
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

            var evt=new JobApplicationCreatedEvent
            {
                JobId=jobId,
                ApplicationId=app.Id,
                ApplicantName =app.ApplicantName,
                ApplicantEmail=app.ApplicantEmail,
                AppliedAt=app.AppliedAt
            };

            await _publisher.PublishAsync(evt,ct);

            await _notifier.NotifyApplicationCreated(new
            {

                id = app.Id,
                JobId = jobId,
                ApplicationId = app.Id,
                ApplicantName = app.ApplicantName,
                ApplicantEmail = app.ApplicantEmail,
                AppliedAt = app.AppliedAt
            });

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

        public async Task<List<JobApplicationDto>> GetApplicationAsync(int jobId, CancellationToken ct = default)
        {
           
            var items = await _repo.GetApplicationsByJobIdAsync(jobId, ct);
            return items.Select(a => new JobApplicationDto(a.Id, a.JobId, a.ApplicantName, a.ApplicantEmail, a.AppliedAt)).ToList();

        }

        public async Task<JobDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var j = await _repo.GetJobApplicationsByIdAsync(id, ct);
            return j is null ? null : new JobDto(j.Id, j.Title, j.Description);

        }
    }
}

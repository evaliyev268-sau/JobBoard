using JobBoard.Application.DTOs;

namespace JobBoard.Application.Abstractions
{
    public interface IRealtimeNotifier
    {
        Task NotifyJobCreated(JobDto job,CancellationToken ct=default);

        Task NotifyApplicationCreated(object payload,CancellationToken ct=default);

        
    }
}

using JobBoard.Application.Abstractions;
using JobBoard.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace JobBoard.Api.Realtime
{
    public class SignalRRealtimeNotifier : IRealtimeNotifier
    {

        private readonly IHubContext<JobsHub> _hub;

        public SignalRRealtimeNotifier(IHubContext<JobsHub> hub)
        {
            _hub = hub;
        }


        public Task NotifyApplicationCreated(object payload, CancellationToken ct = default)
        {
            return _hub.Clients.All.SendAsync("ApplicationCreated", payload, ct);
        }




        public Task NotifyJobCreated(JobDto job, CancellationToken ct = default)
        {
            return _hub.Clients.All.SendAsync("JobCreated", job, ct);

        }







    }
}

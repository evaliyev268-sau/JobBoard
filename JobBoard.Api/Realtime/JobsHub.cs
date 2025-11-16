using Microsoft.AspNetCore.SignalR;

namespace JobBoard.Api.Realtime
{
    public class JobsHub:Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", DateTime.UtcNow);
            await base.OnConnectedAsync();
        }
    }
}

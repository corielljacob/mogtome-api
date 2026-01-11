using Microsoft.AspNetCore.SignalR;
using MogTomeApi.HubClients;

namespace MogTomeApi.Hubs
{
    public class EventsHub : Hub<IHubClient>
    {
    }
}

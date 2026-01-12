using MogTomeApi.Data;

namespace MogTomeApi.HubClients
{
    public interface IHubClient
    {
        Task InformClient(List<Event> events);
    }
}

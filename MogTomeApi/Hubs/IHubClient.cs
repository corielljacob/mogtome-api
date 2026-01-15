using MogTomeApi.Data;

namespace MogTomeApi.Hubs
{
    public interface IHubClient
    {
        Task InformClient(List<Event> events);
    }
}

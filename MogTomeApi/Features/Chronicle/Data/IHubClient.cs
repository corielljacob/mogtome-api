namespace MogTomeApi.Features.Chronicle
{
    public interface IHubClient
    {
        Task InformClient(List<Event> events);
    }
}

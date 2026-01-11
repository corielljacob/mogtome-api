namespace MogTomeApi.HubClients
{
    public interface IHubClient
    {
        Task InformClient(Member message);
    }

    public class Member
    {
        public string name { get; set; }
    }
}

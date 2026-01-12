using MogTomeApi.Data;
using MongoDB.Driver;

namespace MogTomeApi.Services
{
    public class MongoService
    {
        private readonly MongoClient _client;

        public MongoService()
        {
            var connectionString = Environment.GetEnvironmentVariable(Constants.ConnectionStringId, EnvironmentVariableTarget.Machine);

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = Environment.GetEnvironmentVariable(Constants.ConnectionStringId);

            _client = new MongoClient(connectionString);
        }

        public async Task<List<FreeCompanyMember>> GetFreeCompanyMembers()
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var filter = Builders<FreeCompanyMember>.Filter.Empty;
            var freeCompanyMembers = await memberCollection
                .Find(filter)
                .ToListAsync();

            var activeMembers = freeCompanyMembers.Where(member => member.ActiveMember).ToList();
            return activeMembers;
        }

        public async Task<List<Event>> GetFreeCompanyEvents()
        {
            var eventsCollection = _client.GetDatabase("kupo-life").GetCollection<Event>("events");
            var filter = Builders<Event>.Filter.Empty;
            var events = await eventsCollection
                .Find(filter)
                .ToListAsync();

            return events;
        }
    }
}

using MogTomeApi.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using static MogTomeApi.Controllers.EventsController;

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

        public async Task<List<FreeCompanyStaffMember>> GetFreeCompanyStaff()
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyStaffMember>("members");

            var filter = Builders<FreeCompanyStaffMember>.Filter.And(
                Builders<FreeCompanyStaffMember>.Filter.Or(
                    Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.MoogleKnight),
                    Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.PaissaTrainer)
                ),
                Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.ActiveMember, true),
                Builders<FreeCompanyStaffMember>.Filter.Ne(member => member.Name, Constants.PassiveToast)
            );

            var freeCompanyMembers = await memberCollection
                .Find(filter)
                .ToListAsync();

            var activeMembers = freeCompanyMembers.Where(member => member.ActiveMember).ToList();
            return activeMembers;
        }

        public async Task<FreeCompanyMember> GetFreeCompanyMemberByDiscordId(string discordId)
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var filter = Builders<FreeCompanyMember>.Filter.And(
                Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId), 
                Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true)
            );

            var freeCompanyMember = await memberCollection
                .Find(filter)
                .SingleAsync();

            return freeCompanyMember;
        }

        public async Task<PaginatedEventsResponse> GetFreeCompanyEvents(string cursor, int limit)
        {
            var decodedCursor = CursorHelper.DecodeCursor(cursor);

            var filter = Builders<Event>.Filter.Empty;

            if(decodedCursor is not null)
            {
                var createdAtFilter = decodedCursor.CreatedAt;
                var idFilter = ObjectId.Parse(decodedCursor.Id);

                filter = Builders<Event>.Filter.Or(
                    Builders<Event>.Filter.Lt(e => e.CreatedAt, createdAtFilter),

                    Builders<Event>.Filter.And(
                        Builders<Event>.Filter.Eq(e => e.CreatedAt, createdAtFilter),
                        Builders<Event>.Filter.Lt(e => e.Id, idFilter)
                    )
                );
            }

            var eventsCollection = _client.GetDatabase("kupo-life").GetCollection<Event>("events");
            var events = await eventsCollection
                .Find(filter)
                .SortByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id)
                .Limit(limit + 1)
                .ToListAsync();

            bool hasMore = events.Count > limit;
            events = events.Take(limit).ToList();

            string nextCursor = null;
            if(hasMore)
            {
                var lastEvent = events[^1];
                nextCursor = CursorHelper.EncodeCursor(new CursorHelper.EventCursor(lastEvent.CreatedAt, lastEvent.Id.ToString()));
            }

            var paginatedResponse = new PaginatedEventsResponse
            {
                Events = events,
                NextCursor = nextCursor,
                HasMore = hasMore
            };

            return paginatedResponse;
        }
    }
}

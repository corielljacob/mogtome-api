using MongoDB.Bson.Serialization.Attributes;

namespace MogTomeApi.Features.Dashboard.Data
{
    [BsonIgnoreExtraElements]
    public class FreeCompanyMember
    {
        public string Name { get; set; }
        public string CharacterId { get; set; }
        public bool ActiveMember { get; set; }
        public string DiscordId { get; set; }
    }
}

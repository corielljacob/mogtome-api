using MongoDB.Bson.Serialization.Attributes;

namespace MogTomeApi.Features.Dashboard.Data
{
    [BsonIgnoreExtraElements]
    public class DiscordMember
    {
        public string Name { get; set; }
        public string DiscordId { get; set; }
    }
}

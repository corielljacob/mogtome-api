using MongoDB.Bson.Serialization.Attributes;

namespace MogTomeApi.Features.Profile.Data
{
    [BsonIgnoreExtraElements]
    public class FreeCompanyMember
    {
        public string DiscordId { get; set; }
        public string Biography { get; set; }
    }
}

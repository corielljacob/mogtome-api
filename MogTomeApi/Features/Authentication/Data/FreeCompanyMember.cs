using MongoDB.Bson.Serialization.Attributes;

namespace MogTomeApi.Features.Authentication.Data
{
    [BsonIgnoreExtraElements]
    public class FreeCompanyMember
    {
        public string Name { get; set; }
        public string FreeCompanyRank { get; set; }
        public string FreeCompanyRankIcon { get; set; }
        public string CharacterId { get; set; }
        public bool ActiveMember { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string MembershipHistory { get; set; }
        public string AvatarLink { get; set; }
        public string DiscordId { get; set; }
        public bool HasTemporaryKnighthood { get; set; }
        public string Biography { get; set; }
        public DateTime? FirstMogTomeLogin { get; set; }
    }
}

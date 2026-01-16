using MongoDB.Bson.Serialization.Attributes;

namespace MogTomeApi.Data
{
    [BsonIgnoreExtraElements]
    public class MemberToken
    {
        public string DiscordId { get; set; }
        public string CustomRefreshToken { get; set; }
        public bool CustomRefreshTokenRevoked { get; set; }
        public DateTime CustomRefreshTokenExpiresAt { get; set; }
        public string DiscordToken { get; set; }
        public string DiscordRefreshToken { get; set; }
    }
}

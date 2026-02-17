using MongoDB.Bson.Serialization.Attributes;

namespace MogTomeApi.Features.Authentication.Data
{
    [BsonIgnoreExtraElements]
    public class MemberToken
    {
        public string DiscordId { get; set; }
        public string RefreshToken { get; set; }
        public bool RefreshTokenRevoked { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string SessionId { get; set; }
    }
}

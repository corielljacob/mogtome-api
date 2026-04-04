using System.Text.Json.Serialization;

namespace MogTomeApi.Features.Dashboard.Data
{
    public class DiscordUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }
}

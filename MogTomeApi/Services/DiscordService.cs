using MogTomeApi.Data;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MogTomeApi.Services
{
    public class DiscordService
    {
        private readonly HttpClient _httpClient;
        private readonly MongoService _mongoService;
        private readonly IConfiguration _config;
        private readonly ILogger<DiscordService> _logger;
        private readonly string _discordClientId;
        private readonly string _discordClientSecret;
        private readonly string _callbackUri;

        public DiscordService(HttpClient httpClient, IConfiguration config, ILogger<DiscordService> logger, MongoService mongoService)
        {
            _httpClient = httpClient;
            _config = config;
            _discordClientId = Environment.GetEnvironmentVariable("MogTomeClientId", EnvironmentVariableTarget.Process);
            _discordClientSecret = Environment.GetEnvironmentVariable("MogTomeClientSecret", EnvironmentVariableTarget.Process);
            _logger = logger;
            _callbackUri = $"{_config["Authentication:Host"]}/auth/discord/callback";
            _mongoService = mongoService;
        }

        public async Task<DiscordUser> GetDiscordUserInformation(DiscordToken discordToken)
        {
            var expirationPadding = _config.GetValue<int>("Authentication:DiscordTokenExpirationPaddingInSeconds");
            var refreshTokenInDatabase = false;

            if (discordToken.ExpiresIn <= expirationPadding)
            {
                discordToken = await RefreshDiscordToken(discordToken);
                refreshTokenInDatabase = true;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", discordToken.AccessToken);

            var response = await _httpClient.GetAsync("https://discord.com/api/users/@me");
            var responseJson = await response.Content.ReadAsStringAsync();
            var discordUser = JsonSerializer.Deserialize<DiscordUser>(responseJson);

            if(refreshTokenInDatabase)
            {
                await _mongoService.UpdateMemberDiscordToken(discordUser.Id, discordToken);
            }

            return discordUser;
        }

        private async Task<DiscordToken> RefreshDiscordToken(DiscordToken discordToken)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _discordClientId,
                ["client_secret"] = _discordClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = discordToken.RefreshToken
            });

            var response = await _httpClient.PostAsync(_config["Authentication:DiscordTokenUri"], content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh token: {StatusCode} - {Response}", response.StatusCode, body);
                return null;
            }

            return JsonSerializer.Deserialize<DiscordToken>(body);
        }

        public async Task<DiscordToken> GetDiscordTokenUsingCode(string code, string callback = null)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _discordClientId,
                ["client_secret"] = _discordClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = callback ?? _callbackUri
            });

            var response = await _httpClient.PostAsync(_config["Authentication:DiscordTokenUri"], content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange code for token: {StatusCode} - {Response}", response.StatusCode, body);
                return null;
            }

            return JsonSerializer.Deserialize<DiscordToken>(body);
        }
    }
}

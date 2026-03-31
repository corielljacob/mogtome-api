using MediatR;
using MogTomeApi.Features.Authentication.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using static MogTomeApi.Shared.RedirectValidationHelper;

namespace MogTomeApi.Features.Authentication.Commands
{
    public class LoginUser
    {
        public class Command : IRequest<Result<LoginResult>>
        {
            public string State { get; set; }
            public string ExpectedState { get; set; }
            public string RedirectUri { get; set; }
            public string Code { get; set; }
        }

        public class LoginResult
        {
            public string RedirectUri { get; set; }
            public string SessionId { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<LoginResult>>
        {
            private readonly IConfiguration _config;
            private readonly string _discordClientId;
            private readonly string _discordClientSecret;
            private readonly string _callbackUri;
            private readonly HttpClient _httpClient;
            private readonly IMongoDatabase _mongoDatabase;
            private readonly ILogger<Handler> _logger;
            private readonly string _siteUri;

            public Handler(IConfiguration config, HttpClient httpClient, IMongoDatabase mongoDatabase, ILogger<Handler> logger)
            {
                _config = config;
                _discordClientId = Environment.GetEnvironmentVariable("MogTomeClientId", EnvironmentVariableTarget.Process);
                _discordClientSecret = Environment.GetEnvironmentVariable("MogTomeClientSecret", EnvironmentVariableTarget.Process);
                _callbackUri = $"{_config["Authentication:Host"]}/auth/discord/callback";
                _httpClient = httpClient;
                _mongoDatabase = mongoDatabase;
                _logger = logger;
                _siteUri = _config["Authentication:SiteUri"];
            }

            public async Task<Result<LoginResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (request.State != request.ExpectedState)
                {
                    return new Result<LoginResult>(null, HttpStatusCode.InternalServerError, false, "State validation failed");
                }

                var discordToken = await GetDiscordTokenUsingCode(request.Code);

                if (discordToken == null)
                {
                    return new Result<LoginResult>(null, HttpStatusCode.InternalServerError, false, "Discord token acquisition with code failed");
                }

                var discordUser = await GetDiscordUserInformation(discordToken);
                var freeCompanyMember = await GetFreeCompanyMemberByDiscordId(discordUser.Id);

                if (freeCompanyMember == null)
                {
                    return new Result<LoginResult>(null, HttpStatusCode.Forbidden, false, "No active free company member found for this Discord user");
                }

                if (freeCompanyMember.FirstMogTomeLogin == null)
                {
                    var loginDate = DateTime.UtcNow;
                    await SetFirstTimeMogTomeLogin(discordUser.Id, loginDate);
                    freeCompanyMember = await GetFreeCompanyMemberByDiscordId(discordUser.Id);
                }

                var jwtTokenIssuer = _config["Authentication:Host"];
                var jwtTokenAudience = _config["Authentication:Audience"];
                var jwt = JwtHelper.CreateAccessToken(freeCompanyMember, jwtTokenIssuer, jwtTokenAudience);
                var refreshToken = JwtHelper.CreateRefreshToken();
                var sessionId = SessionHelper.GenerateSessionId();

                await UpsertMemberToken(discordUser.Id, refreshToken, sessionId);

                string redirectUri = request.RedirectUri;
                var allowedHosts = _config["Authentication:AllowedRedirectHosts"].Split(';');
                if (string.IsNullOrEmpty(redirectUri) || DetermineIfRedirectIsValid(redirectUri, allowedHosts) == false)
                {
                    redirectUri = _config["Authentication:SiteUri"];
                }

                var loginResult = new LoginResult
                {
                    RedirectUri = $"{redirectUri}?token={jwt}",
                    SessionId = sessionId
                };

                return new Result<LoginResult>(loginResult, HttpStatusCode.OK, true, null);
            }

            private async Task<DiscordToken> GetDiscordTokenUsingCode(string code, string callback = null)
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

            private async Task<DiscordUser> GetDiscordUserInformation(DiscordToken discordToken)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", discordToken.AccessToken);

                var response = await _httpClient.GetAsync("https://discord.com/api/users/@me");
                var responseJson = await response.Content.ReadAsStringAsync();
                var discordUser = JsonSerializer.Deserialize<DiscordUser>(responseJson);

                return discordUser;
            }

            private async Task<FreeCompanyMember> GetFreeCompanyMemberByDiscordId(string discordId)
            {
                var memberCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");

                var filter = Builders<FreeCompanyMember>.Filter.And(
                    Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId),
                    Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true)
                );

                var member = await memberCollection.Find(m => m.DiscordId == discordId).FirstOrDefaultAsync();
                return member;
            }

            public async Task SetFirstTimeMogTomeLogin(string discordId, DateTime loginDate)
            {
                var membersCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");

                var filter = Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId);

                var update = Builders<FreeCompanyMember>.Update
                    .Set(member => member.FirstMogTomeLogin, loginDate);

                await membersCollection.UpdateOneAsync(filter, update);
            }

            private async Task UpsertMemberToken(string discordId, JwtHelper.RefreshToken refreshToken, string sessionId)
            {
                var memberToken = new MemberToken
                {
                    DiscordId = discordId,
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                    RefreshTokenRevoked = refreshToken.Revoked,
                    SessionId = sessionId
                };

                var tokenCollection = _mongoDatabase.GetCollection<MemberToken>("tokens");
                var filter = Builders<MemberToken>.Filter.And(
                    Builders<MemberToken>.Filter.Eq(token => token.DiscordId, discordId),
                    Builders<MemberToken>.Filter.Eq(token => token.SessionId, sessionId)
                );

                await tokenCollection.ReplaceOneAsync(filter, memberToken, new ReplaceOptions { IsUpsert = true });
            }
        }
    }
}

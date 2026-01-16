using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MogTomeApi.Data;
using MogTomeApi.Services;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MogTomeApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly MongoService _mongoService;
        private readonly HttpClient _httpClient;
        private readonly string _discordClientId;
        private readonly string _discordClientSecret;
        private readonly string _callbackUri;
        private readonly string _siteUri;
        private readonly IConfiguration _config;

        public AuthenticationController(ILogger<AuthenticationController> logger, MongoService mongoService, HttpClient httpClient, IConfiguration config)
        {
            _logger = logger;
            _mongoService = mongoService;
            _httpClient = httpClient;
            _discordClientId = Environment.GetEnvironmentVariable("MogTomeClientId", EnvironmentVariableTarget.Process);
            _discordClientSecret = Environment.GetEnvironmentVariable("MogTomeClientSecret", EnvironmentVariableTarget.Process);
            _config = config;
            _callbackUri = $"{_config["Authentication:Host"]}/auth/discord/callback";
            _siteUri = _config["Authentication:SiteUri"];
        }

        [HttpGet("discord/login")]
        public IActionResult Login([FromQuery] string redirect)
        {
            HttpContext.Session.Clear();
            var isValidRedirect = DetermineIfRedirectIsValid(redirect);

            if(isValidRedirect)
            {
                HttpContext.Session.SetString("redirect", redirect);
            }
            else
            {
                HttpContext.Session.SetString("redirect", _siteUri);
            }

            var state = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("discord_oauth_state", state);

            var scopes = Uri.EscapeDataString(_config["Authentication:Scopes"]);

            var url =
                $"{_config["Authentication:DiscordLoginUri"]}" +
                $"?client_id={_discordClientId}" +
                $"&redirect_uri={_callbackUri}" +
                $"&response_type=code" +
                $"&scope={scopes}" +
                $"&state={state}";

            return Redirect(url);
        }

        [HttpGet("discord/callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            var expectedState = HttpContext.Session.GetString("discord_oauth_state");

            if (state != expectedState)
            {
                return BadRequest("State validation failed");
            }

            var token = await ExchangeCodeForToken(code);

            if(token == null)
            {
                return Redirect(_siteUri);
            }

            // Fetch discord info to populate identity
            var discordUser = await GetDiscordUserInformation(token.AccessToken);

            // Using discord identity, create a JWT for the FE to use
            var jwt = await CreateJwtFromDiscordUser(discordUser);

            // Process redirect if stored in session state
            var redirectUri = HttpContext.Session.GetString("redirect");
            if(string.IsNullOrEmpty(redirectUri) || DetermineIfRedirectIsValid(redirectUri) == false)
            {
                redirectUri = _config["Authentication:SiteUri"];
            }

            redirectUri = $"{redirectUri}?token={jwt}";
            return Redirect(redirectUri);
        }

        private async Task<DiscordToken> ExchangeCodeForToken(string code)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _discordClientId,
                ["client_secret"] = _discordClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _callbackUri
            });

            var response = await _httpClient.PostAsync(_config["Authentication:DiscordTokenUri"], content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange code for token: {StatusCode} - {Response}", response.StatusCode, json);
                return null;
            }

            return JsonSerializer.Deserialize<DiscordToken>(json);
        }

        private async Task<DiscordUser> GetDiscordUserInformation(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync("https://discord.com/api/users/@me");
            var responseJson = await response.Content.ReadAsStringAsync();
            var discordUser = JsonSerializer.Deserialize<DiscordUser>(responseJson);

            return discordUser;
        }

        private async Task<string> CreateJwtFromDiscordUser(DiscordUser discordUser)
        {
            var freeCompanyMember = await _mongoService.GetFreeCompanyMemberByDiscordId(discordUser.Id);

            var claims = new Dictionary<string, object>
            {
                ["memberName"] = freeCompanyMember.Name,
                ["memberRank"] = freeCompanyMember.FreeCompanyRank,
                ["memberPortraitUrl"] = freeCompanyMember.AvatarLink
            };

            var secretKey = Environment.GetEnvironmentVariable("MogTomeApiSigningSecret", EnvironmentVariableTarget.Process);
            var signingkey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _config["Authentication:Host"],
                Audience = _config["Authentication:Audience"],
                Claims = claims,
                IssuedAt = null,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(signingkey, SecurityAlgorithms.HmacSha256Signature)
            };

            var jwtHandler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
            var tokenString = jwtHandler.CreateToken(descriptor);

            return tokenString;
        }

        private bool DetermineIfRedirectIsValid(string redirect)
        {
            if (string.IsNullOrEmpty(redirect))
                return false;

            if(Uri.TryCreate(redirect, UriKind.Absolute, out var redirectUri) == false)
            {
                return false;
            }

            if (redirectUri.Scheme != Uri.UriSchemeHttps)
                return false;

            if (string.IsNullOrEmpty(redirectUri.UserInfo) == false)
                return false;

            var allowedHosts = _config["Authentication:AllowedRedirectHosts"].Split(';');
            if (allowedHosts.Contains(redirectUri.Host, StringComparer.OrdinalIgnoreCase) == false)
            {
                return false;
            }

            return true;
        }
    }
}

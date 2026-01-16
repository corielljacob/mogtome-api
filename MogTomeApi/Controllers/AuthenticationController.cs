using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Services;
using System.Text;

namespace MogTomeApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly MongoService _mongoService;
        private readonly JwtService _jwtService;
        private readonly DiscordService _discordService;
        private readonly string _discordClientId;
        private readonly string _callbackUri;
        private readonly string _siteUri;
        private readonly IConfiguration _config;

        public AuthenticationController(ILogger<AuthenticationController> logger, MongoService mongoService, IConfiguration config, JwtService jwtService, DiscordService discordService)
        {
            _logger = logger;
            _mongoService = mongoService;
            _discordClientId = Environment.GetEnvironmentVariable("MogTomeClientId", EnvironmentVariableTarget.Process);
            _config = config;
            _callbackUri = $"{_config["Authentication:Host"]}/auth/discord/callback";
            _siteUri = _config["Authentication:SiteUri"];
            _jwtService = jwtService;
            _discordService = discordService;
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
            try
            {
                var expectedState = HttpContext.Session.GetString("discord_oauth_state");

                if (state != expectedState)
                {
                    return BadRequest("State validation failed");
                }

                var token = await _discordService.GetDiscordTokenUsingCode(code);

                if (token == null)
                {
                    return Redirect(_siteUri);
                }

                // Fetch discord info to populate identity
                var discordUser = await _discordService.GetDiscordUserInformation(token);

                // Using discord identity, create a JWT for the FE to use
                var jwt = await _jwtService.CreateAccessToken(discordUser.Id);
                var refreshToken = JwtService.CreateRefreshToken();

                // Save token info to database
                await _mongoService.UpsertMemberToken(discordUser.Id, refreshToken, token);
                WriteRefreshTokenToCookie(refreshToken.Token);

                // Process redirect if stored in session state
                var redirectUri = HttpContext.Session.GetString("redirect");
                if (string.IsNullOrEmpty(redirectUri) || DetermineIfRedirectIsValid(redirectUri) == false)
                {
                    redirectUri = _config["Authentication:SiteUri"];
                }

                redirectUri = $"{redirectUri}?token={jwt}";
                return Redirect(redirectUri);
            } 
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error during Discord OAuth callback");
                return Redirect(_siteUri);
            }
        }

        [HttpGet("discord/refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (refreshToken == null)
                return Unauthorized();

            var memberToken = await _mongoService.GetMemberRefreshTokenInfo(refreshToken);

            if(memberToken == null || memberToken.CustomRefreshTokenRevoked || memberToken.CustomRefreshTokenExpiresAt <= DateTime.UtcNow)
                return Unauthorized();

            var newRefreshToken = JwtService.CreateRefreshToken();
            var newJwt = await _jwtService.CreateAccessToken(memberToken.DiscordId);

            await _mongoService.UpdateMemberRefreshToken(memberToken.DiscordId, newRefreshToken);
            WriteRefreshTokenToCookie(newRefreshToken.Token);

            return Ok(new
            {
                token = newJwt
            });
        }

        private void WriteRefreshTokenToCookie(string refreshToken)
        {
            Response.Cookies.Append(
                "refresh_token",
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(14)
                }
            );
        }

        private bool DetermineIfRedirectIsValid(string redirect)
        {
            if (string.IsNullOrEmpty(redirect))
                return false;

            if(Uri.TryCreate(redirect, UriKind.Absolute, out var redirectUri) == false)
            {
                return false;
            }

            if (redirectUri.Scheme != Uri.UriSchemeHttps && redirectUri.Host != "localhost")
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

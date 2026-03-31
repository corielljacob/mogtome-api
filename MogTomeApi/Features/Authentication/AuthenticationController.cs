using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MogTomeApi.Features.Authentication.Commands;
using static MogTomeApi.Shared.RedirectValidationHelper;

namespace MogTomeApi.Features.Authentication
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly string _discordClientId;
        private readonly string _callbackUri;
        private readonly string _swaggerCallbackUri;
        private readonly string _siteUri;
        private readonly IConfiguration _config;
        private readonly IMediator _mediator;

        public AuthenticationController(
            ILogger<AuthenticationController> logger, 
            IConfiguration config,
            IMediator mediator)
        {
            _logger = logger;
            _discordClientId = Environment.GetEnvironmentVariable("MogTomeClientId", EnvironmentVariableTarget.Process);
            _config = config;
            _callbackUri = $"{_config["Authentication:Host"]}/auth/discord/callback";
            _swaggerCallbackUri = $"{_config["Authentication:Host"]}/auth/discord/swagger-callback";
            _siteUri = _config["Authentication:SiteUri"];
            _mediator = mediator;
        }

        [HttpGet("discord/login")]
        public IActionResult Login([FromQuery] string redirect)
        {
            var allowedHosts = _config["Authentication:AllowedRedirectHosts"].Split(';');
            var isValidRedirect = DetermineIfRedirectIsValid(redirect, allowedHosts);

            if (isValidRedirect)
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
                var redirectUri = HttpContext.Session.GetString("redirect");

                var input = new LoginUser.Command
                {
                    Code = code,
                    State = state,
                    ExpectedState = expectedState,
                    RedirectUri = redirectUri
                };

                var output = await _mediator.Send(input);

                if (output.IsSuccess && string.IsNullOrEmpty(output.Value.RedirectUri) == false)
                {
                    WriteSessionIdToCookie(output.Value.SessionId);
                    return Redirect(output.Value.RedirectUri);
                }
                else
                {
                    return Redirect($"{_siteUri}?missingUserData=true");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Discord OAuth callback");
                return Redirect($"{_siteUri}?missingUserData=true");
            }
        }

        [HttpGet("discord/refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                var sessionId = Request.Cookies["mogtome_session_id"];

                if (sessionId == null)
                    return Unauthorized();

                var input = new RefreshUserSession.Command
                {
                    SessionId = sessionId
                };

                var output = await _mediator.Send(input);

                if (!output.IsSuccess)
                    return Unauthorized();

                WriteSessionIdToCookie(output.Value.SessionId);

                return Ok(new
                {
                    token = output.Value.Jwt
                });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error during session refresh");
                return Unauthorized();
            }
        }

        [HttpGet("discord/swagger-login")]
        public IActionResult SwaggerLogin()
        {
            var state = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("discord_oauth_state", state);
            HttpContext.Session.SetString("redirect", $"{_config["Authentication:Host"]}/discord-complete.html");

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

        private void WriteSessionIdToCookie(string sessionId)
        {
            Response.Cookies.Delete("mogtome_session_id", new CookieOptions
            {
                Domain = _config["Authentication:CookieDomain"],
                Path = "/"
            });

            Response.Cookies.Delete("mogtome_session_id");

            Response.Cookies.Append(
                "mogtome_session_id",
                sessionId,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(14),
                    Domain = _config["Authentication:CookieDomain"],
                    Path = "/"
                }
            );
        }
    }
}

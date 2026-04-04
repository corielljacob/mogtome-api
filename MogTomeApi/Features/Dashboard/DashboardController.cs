using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Features.Dashboard.Commands;
using MogTomeApi.Features.Dashboard.Queries;
using MogTomeApi.Shared;

namespace MogTomeApi.Features.Dashboard
{
    [ApiController]
    [Route("dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IMediator _mediator;

        public DashboardController(ILogger<DashboardController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("unmapped-characters")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<IActionResult> GetUnmappedCharacters([FromQuery] string discordUsername)
        {
            try
            {
                var input = new GetUnmappedCharacters.Query
                {
                    DiscordUsername = discordUsername
                };

                var result = await _mediator.Send(input);

                if(result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to fetch unmapped characters: {Error}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching unmapped characters: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching unmapped characters");
            }
        }

        [HttpGet("unmapped-discord-users")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<IActionResult> GetUnmappedDiscordUsers([FromQuery] string characterName)
        {
            try
            {
                if(string.IsNullOrEmpty(characterName) == false && characterName.Contains(' ') == false)
                {
                    return BadRequest("characterName must contain a space");
                }

                var input = new GetUnmappedDiscordUsers.Query
                {
                    CharacterName = characterName
                };

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);

                }
                else
                {
                    _logger.LogError("Failed to fetch unmapped discord users: {Error}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching unmapped discord users: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching unmapped discord users");
            }
        }

        [HttpPost("map")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<IActionResult> Map([FromBody] MapDiscordAccountToCharacter.Command input)
        {
            try
            {
                var result = await _mediator.Send(input);
                
                if(result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to map discord account to character: {Error}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Error mapping discord account to character: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when mapping the discord account to the character");
            }
        }

        [HttpPost("unlink")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<IActionResult> Unlink([FromBody] UnlinkDiscordAccountFromCharacter.UnlinkDiscordAccountFromCharacterCommand input)
        {
            try
            {
                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to map discord account to character: {Error}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error mapping discord account to character: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when mapping the discord account to the character");
            }
        }

        [HttpGet("mapped-characters")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<IActionResult> GetMappedCharacters()
        {
            try
            {
                var result = await _mediator.Send(new GetMappedCharactersQuery());

                if (result.IsSuccess)
                {
                    return Ok(result.Value);

                }
                else
                {
                    _logger.LogError("Failed to fetch mapped characters: {Error}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching mapped characters: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching mapped characters");
            }
        }
    }
}

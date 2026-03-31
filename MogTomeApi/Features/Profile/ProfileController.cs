using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Features.Profile.Commands;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Features.Profile.Queries;
using MogTomeApi.Shared;

namespace MogTomeApi.Features.Profile
{
    [ApiController]
    [Route("profile")]
    public class ProfileController : ControllerBase
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IMediator _mediator;

        public ProfileController(ILogger<ProfileController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpPost("biography")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<IActionResult> SetBiography([FromBody] string biography)
        {
            try
            {
                if (string.IsNullOrEmpty(biography) || biography.Length > 500)
                {
                    return BadRequest("Biography must be between 1 and 500 characters");
                }

                var input = new SetBiography.Command
                {
                    Biography = biography,
                    DiscordId = User.FindFirst("discordId")?.Value
                };

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok();

                }
                else
                {
                    _logger.LogError("Failed to set biography for user: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to add biography for user: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when setting the users biography");
            }
        }

        [HttpPost("biography/submission")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<IActionResult> CreateBiographySubmission([FromBody] string biography)
        {
            try
            {
                if (string.IsNullOrEmpty(biography) || biography.Length > 500)
                {
                    return BadRequest("Biography must be between 1 and 500 characters");
                }

                var input = new CreateBiographySubmission.Command
                {
                    Biography = biography,
                    DiscordId = User.FindFirst("discordId")?.Value
                };

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok();

                }
                else
                {
                    _logger.LogError("Failed to create biography submission for user: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create biography submission for user: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when creating the biography submission");
            }
        }

        [HttpGet("biography/submission")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<ActionResult<BiographySubmission>> GetPendingBiographySubmissions()
        {
            try
            {
                var input = new GetPendingBiographySubmissions.Query();

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to retrieve biography submissions: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve biography submissions: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when retrieving biography submissions");
            }
        }

        [HttpGet("biography/submission/{memberId}")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<ActionResult<BiographySubmission>> GetSubmissionForMember([FromRoute] string memberId)
        {
            try
            {
                var input = new GetSubmissionForMember.Query
                {
                    DiscordId = memberId
                };

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to retrieve biography submission: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve biography submission: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when retrieving biography submission");
            }
        }

        [HttpPost("biography/submission/{submissionId}")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<ActionResult<BiographySubmission>> EditSubmission([FromRoute] Guid submissionId, [FromBody] string biography)
        {
            try
            {
                var input = new EditSubmission.Command
                {
                    SubmissionId = submissionId,
                    Biography = biography,
                    DiscordId = User.FindFirst("discordId")?.Value
                };

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to retrieve biography submission: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve biography submission: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when retrieving biography submission");
            }
        }

        [HttpPost("biography/submission/approve/{submissionId}")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<ActionResult<BiographySubmission>> ApproveBiographySubmission([FromRoute] Guid submissionId)
        {
            try
            {
                var input = new ApproveBiographySubmission.Command
                {
                    SubmissionId = submissionId,
                    ApprovedByDiscordId = User.FindFirst("discordId")?.Value
                };

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to approve biography submission: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to approve biography submission: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when approving biography submission");
            }
        }

        [HttpPost("biography/submission/reject/{submissionId}")]
        [Authorize(Roles = Constants.MoogleKnight)]
        public async Task<ActionResult<BiographySubmission>> RejectBiographySubmission([FromRoute] Guid submissionId)
        {
            try
            {
                var input = new RejectBiographySubmission.Command
                {
                    SubmissionId = submissionId,
                    RejectedByDiscordId = User.FindFirst("discordId")?.Value
                };

                var result = await _mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to reject biography submission: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to reject biography submission: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when rejecting biography submission");
            }
        }
    }
}

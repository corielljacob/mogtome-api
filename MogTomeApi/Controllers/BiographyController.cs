using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Data;
using MogTomeApi.Services;
using System.Net;

namespace MogTomeApi.Controllers
{
    [ApiController]
    [Route("biography")]
    public class BiographyController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly MongoService _mongoService;

        public BiographyController(ILogger<EventsController> logger, MongoService mongoService)
        {
            _logger = logger;
            _mongoService = mongoService;
        }

        [HttpPost()]
        [Authorize]
        public async Task<IActionResult> CreateBiography([FromBody] CreateBiographyRequest biographyRequest)
        {
            try
            {
                var memberRank = User.FindFirst("memberRank")?.Value;
                var discordId = User.FindFirst("discordId")?.Value;
                bool.TryParse(User.FindFirst("hasKnighthood")?.Value, out var hasKnighthood);
                bool.TryParse(User.FindFirst("hasTemporaryKnighthood")?.Value, out var hasTemporaryKnighthood);

                if(discordId == null)
                {
                    return Forbid();
                }

                if (hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid();
                }

                if(string.IsNullOrEmpty(biographyRequest.Biography) || biographyRequest.Biography.Length > 500)
                {
                    return BadRequest("Biography must be between 1 and 500 characters");
                }

                await _mongoService.SetUserBiography(discordId, biographyRequest.Biography);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to add biography for user: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when setting the users biography");
            }
        }

        [HttpPost("/submission")]
        [Authorize]
        public async Task<IActionResult> CreateBiographySubmission([FromBody] CreateBiographyRequest biographyRequest)
        {
            try
            {
                var memberRank = User.FindFirst("memberRank")?.Value;
                var discordId = User.FindFirst("discordId")?.Value;
                bool.TryParse(User.FindFirst("hasKnighthood")?.Value, out var hasKnighthood);
                bool.TryParse(User.FindFirst("hasTemporaryKnighthood")?.Value, out var hasTemporaryKnighthood);

                if (discordId == null)
                {
                    return Forbid();
                }

                if (memberRank != "Paissa Trainer" && hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid();
                }

                if (string.IsNullOrEmpty(biographyRequest.Biography) || biographyRequest.Biography.Length > 500)
                {
                    return BadRequest("Biography must be between 1 and 500 characters");
                }

                await _mongoService.CreateBiographySubmission(discordId, biographyRequest.Biography);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create biography submission for user: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when creating the biography submission");
            }
        }

        [HttpGet("/submission")]
        [Authorize]
        public async Task<ActionResult<BiographySubmission>> GetPendingBiographySubmissions()
        {
            try
            {
                var memberRank = User.FindFirst("memberRank")?.Value;
                var discordId = User.FindFirst("discordId")?.Value;
                bool.TryParse(User.FindFirst("hasKnighthood")?.Value, out var hasKnighthood);
                bool.TryParse(User.FindFirst("hasTemporaryKnighthood")?.Value, out var hasTemporaryKnighthood);

                if (discordId == null)
                {
                    return Forbid();
                }

                if (hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid();
                }

                var submissions = await _mongoService.GetPendingBiographySubmissions();
                return Ok(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve biography submissions: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when retrieving biography submissions");
            }
        }

        [HttpGet("/submission/{memberId}")]
        [Authorize]
        public async Task<ActionResult<BiographySubmission>> GetUserSubmissionInfo([FromRoute] string memberId)
        {
            try
            {
                var memberRank = User.FindFirst("memberRank")?.Value;
                var discordId = User.FindFirst("discordId")?.Value;
                bool.TryParse(User.FindFirst("hasKnighthood")?.Value, out var hasKnighthood);
                bool.TryParse(User.FindFirst("hasTemporaryKnighthood")?.Value, out var hasTemporaryKnighthood);

                if (discordId == null)
                {
                    return Forbid();
                }

                if (memberRank != "Paissa Trainer" && hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid();
                }

                var submission = await _mongoService.GetUserSubmissionInfo(memberId);

                if(submission is null)
                {
                    return NoContent();
                }
                else
                {
                    return Ok(submission);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve biography submissions: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when retrieving biography submissions");
            }
        }

        [HttpPost("/submission/edit/{submissionId}")]
        [Authorize]
        public async Task<ActionResult<BiographySubmission>> EditSubmission([FromRoute] Guid submissionId, [FromBody] CreateBiographyRequest createBiographyRequest)
        {
            try
            {
                var memberRank = User.FindFirst("memberRank")?.Value;
                var discordId = User.FindFirst("discordId")?.Value;
                bool.TryParse(User.FindFirst("hasKnighthood")?.Value, out var hasKnighthood);
                bool.TryParse(User.FindFirst("hasTemporaryKnighthood")?.Value, out var hasTemporaryKnighthood);

                if (discordId == null)
                {
                    return Forbid();
                }

                if (memberRank != "Paissa Trainer" && hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid();
                }

                var submission = await _mongoService.EditSubmission(submissionId, createBiographyRequest.Biography);

                if (submission == HttpStatusCode.NotFound)
                {
                    return NotFound("Submission not found");
                }
                else
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve biography submissions: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when retrieving biography submissions");
            }
        }

        [HttpPost("/submission/approve/{submissionId}")]
        [Authorize]
        public async Task<IActionResult> ApproveSubmission([FromRoute] Guid submissionId)
        {
            try
            {
                var memberRank = User.FindFirst("memberRank")?.Value;
                var discordId = User.FindFirst("discordId")?.Value;
                bool.TryParse(User.FindFirst("hasKnighthood")?.Value, out var hasKnighthood);
                bool.TryParse(User.FindFirst("hasTemporaryKnighthood")?.Value, out var hasTemporaryKnighthood);

                if (discordId == null)
                {
                    return Forbid();
                }

                if (hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid();
                }

                var approveResult = await _mongoService.ApproveSubmission(submissionId, discordId);
                if (approveResult.Equals(HttpStatusCode.NotFound))
                {
                    return NotFound("Submission not found");
                }
                else if (approveResult.Equals(HttpStatusCode.InternalServerError))
                {
                    return StatusCode(500, "An error occurred when approving the submission");
                }
                else
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to approve biography submission for user: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when approving the biography submission");
            }
        }

        [HttpPost("/submission/reject/{submissionId}")]
        [Authorize]
        public async Task<IActionResult> RejectSubmission([FromRoute] Guid submissionId)
        {
            try
            {
                var memberRank = User.FindFirst("memberRank")?.Value;
                var discordId = User.FindFirst("discordId")?.Value;
                bool.TryParse(User.FindFirst("hasKnighthood")?.Value, out var hasKnighthood);
                bool.TryParse(User.FindFirst("hasTemporaryKnighthood")?.Value, out var hasTemporaryKnighthood);

                if (discordId == null)
                {
                    return Forbid();
                }

                if (hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid();
                }

                var rejectResult = await _mongoService.RejectSubmission(submissionId, discordId);
                if (rejectResult.Equals(HttpStatusCode.NotFound))
                {
                    return NotFound("Submission not found");
                }
                else if (rejectResult.Equals(HttpStatusCode.InternalServerError))
                {
                    return StatusCode(500, "An error occurred when rejecting the submission");
                }
                else
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to reject biography submission for user: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when rejecting the biography submission");
            }
        }

        public class CreateBiographyRequest
        {
            public string Biography { get; set; }
        }
    }
}

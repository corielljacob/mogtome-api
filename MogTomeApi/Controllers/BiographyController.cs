using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Data;
using MogTomeApi.Services;

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
                    return Forbid("Only users with linked characters may update their biography");
                }

                if (hasKnighthood == false && hasTemporaryKnighthood == false)
                {
                    return Forbid("Only Moogle Knights and Moogle Guardians are authorized to set biographies");
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

        public class CreateBiographyRequest
        {
            public string Biography { get; set; }
        }
    }
}

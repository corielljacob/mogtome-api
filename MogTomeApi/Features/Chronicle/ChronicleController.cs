using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MogTomeApi.Features.Chronicle.Queries;

namespace MogTomeApi.Features.Chronicle
{
    [ApiController]
    [Route("events")]
    public class ChronicleController : ControllerBase
    {
        private readonly ILogger<ChronicleController> _logger;
        private readonly IHubContext<EventsHub, IHubClient> _eventsHub;
        private readonly IMediator mediator;

        public ChronicleController(ILogger<ChronicleController> logger, IHubContext<EventsHub, IHubClient> eventsHubContext, IMediator mediator)
        {
            _logger = logger;
            _eventsHub = eventsHubContext;
            this.mediator = mediator;
        }

        [HttpGet()]
        [Authorize]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string cursor = null,
            [FromQuery] int limit = 100,
            [FromQuery] string query = null,
            [FromQuery] EventType? filter = null)
        {
            try
            {
                var input = new GetEvents.Query
                {
                    Cursor = cursor,
                    Limit = limit,
                    QueryString = query,
                    EventTypeFilter = filter.ToString()
                };

                var result = await mediator.Send(input);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                else
                {
                    _logger.LogError("Failed to fetch chronicle events: {Error}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching chronicle events: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching chronicle events");
            }
        }

        [HttpPost("create-event")]
        public IActionResult CreateEvent([FromBody] List<Event> events)
        {
            HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKeyValues);
            var apiKey = apiKeyValues.ToString().ToUpper();
            var expectedApiKey = Environment.GetEnvironmentVariable("MogTomeApiKey").ToUpper();

            if (apiKey != expectedApiKey)
            {
                _logger.LogWarning("Unauthorized attempt to create event.");
                return StatusCode(StatusCodes.Status401Unauthorized, "You are not authorized to create an event");
            }

            _eventsHub.Clients.All.InformClient(events);
            return Ok();
        }
    }
}

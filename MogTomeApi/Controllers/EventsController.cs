using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MogTomeApi.Data;
using MogTomeApi.Hubs;
using MogTomeApi.Services;

namespace MogTomeApi.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private IHubContext<EventsHub, IHubClient> _eventsHub;
        private readonly MongoService _mongoService;

        public EventsController(ILogger<EventsController> logger, IHubContext<EventsHub, IHubClient> eventsHubContext, MongoService mongoService)
        {
            _logger = logger;
            _eventsHub = eventsHubContext;
            _mongoService = mongoService;
        }

        [HttpGet()]
        [Authorize]
        public async Task<ActionResult<PaginatedEventsResponse>> GetEvents(
            [FromQuery] string cursor = null, 
            [FromQuery] int limit = 100, 
            [FromQuery] string query = null, 
            [FromQuery] EventType? filter = null)
        {
            try
            {
                var events = await _mongoService.GetFreeCompanyEvents(cursor, limit, query, filter.ToString());
                return Ok(events);
            }
            catch(Exception ex)
            {
                _logger.LogError("Error fetching events: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching events");
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

        public class PaginatedEventsResponse
        {
            public List<Event> Events { get; set; }
            public string NextCursor { get; set; }
            public bool HasMore { get; set; }
        }
    }
}

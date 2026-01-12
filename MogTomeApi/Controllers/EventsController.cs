using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MogTomeApi.Data;
using MogTomeApi.HubClients;
using MogTomeApi.Hubs;
using MogTomeApi.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Threading.Tasks;

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
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }

        [HttpGet()]
        public async Task<IEnumerable<Event>> GetEvents()
        {
            var events = await _mongoService.GetFreeCompanyEvents();
            return events;
        }

        [HttpPost("create-event")]
        public void CreateEvent([FromBody] List<Event> events)
        {
            HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var apiKeyValues);
            var apiKey = apiKeyValues.ToString().ToUpper();
            var expectedApiKey = Environment.GetEnvironmentVariable("MogTomeApiKey").ToUpper();

            if (apiKey != expectedApiKey)
            {
                _logger.LogWarning("Unauthorized attempt to create event.");
                HttpContext.Response.StatusCode = 401; // Unauthorized
                return;
            }

            _eventsHub.Clients.All.InformClient(events);
        }
    }
}

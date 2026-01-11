using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MogTomeApi.HubClients;
using MogTomeApi.Hubs;

namespace MogTomeApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private IHubContext<EventsHub, IHubClient> _eventsHub;

        public EventsController(ILogger<EventsController> logger, IHubContext<EventsHub, IHubClient> eventsHubContext)
        {
            _logger = logger;
            _eventsHub = eventsHubContext;
        }

        [HttpGet(Name = "GetEvents")]
        public IEnumerable<string> GetEvents()
        {
            return new List<string> { "boy" };
        }

        [HttpGet("CreateEvent")]
        public void CreateEvent()
        {
            _eventsHub.Clients.All.InformClient(new Member { name = "Jacob"});
        }
    }
}

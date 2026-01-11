using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Data;
using MogTomeApi.Services;

namespace MogTomeApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly MongoService _mongoService;

        public MembersController(ILogger<EventsController> logger, MongoService mongoService)
        {
            _logger = logger;
            _mongoService = mongoService;
        }

        [HttpGet(Name = "GetMembers")]
        public async Task<Response> GetMembers()
        {
            var members = await _mongoService.GetFreeCompanyMembers();

            var response = new Response
            {
                Members = members,
                TotalCount = members.Count
            };
            
            return response;
        }
    }
    
    public class Response
    {
        public int TotalCount { get; set; }
        public required IEnumerable<FreeCompanyMember> Members { get; set; }
    }
}

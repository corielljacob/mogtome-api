using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Data;
using MogTomeApi.Services;

namespace MogTomeApi.Controllers
{
    [ApiController]
    [Route("members")]
    public class MembersController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly MongoService _mongoService;

        public MembersController(ILogger<EventsController> logger, MongoService mongoService)
        {
            _logger = logger;
            _mongoService = mongoService;
        }

        [HttpGet()]
        public async Task<ActionResult<GetMembersResponse>> GetMembers()
        {
            try
            {
                var members = await _mongoService.GetFreeCompanyMembers();

                var memberResponse = members.Select(member => new Member
                {
                    Name = member.Name,
                    FreeCompanyRank = member.FreeCompanyRank,
                    FreeCompanyRankIcon = member.FreeCompanyRankIcon,
                    CharacterId = member.CharacterId,
                    ActiveMember = member.ActiveMember,
                    AvatarLink = member.AvatarLink
                });
                
                var response = new GetMembersResponse
                {
                    Members = memberResponse,
                    TotalCount = members.Count
                };

                return Ok(response);
            }
            catch(Exception ex)
            {
                _logger.LogError("Error fetching members: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching members");
            }
        }

        [HttpGet("staff")]
        public async Task<ActionResult<GetStaffResponse>> GetStaff()
        {
            try
            {
                var staff = await _mongoService.GetFreeCompanyStaff();

                var response = new GetStaffResponse
                {
                    Staff = staff,
                    TotalCount = staff.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching staff: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching staff");
            }
        }
    }
    
    public class GetMembersResponse
    {
        public int TotalCount { get; set; }
        public required IEnumerable<Member> Members { get; set; }
    }

    public class Member
    {
        public string Name { get; set; }
        public string FreeCompanyRank { get; set; }
        public string FreeCompanyRankIcon { get; set; }
        public string CharacterId { get; set; }
        public bool ActiveMember { get; set; }
        public string AvatarLink { get; set; }
    }

    public class GetStaffResponse
    {
        public int TotalCount { get; set; }
        public required IEnumerable<FreeCompanyStaffMember> Staff { get; set; }
    }
}

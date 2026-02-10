using Microsoft.AspNetCore.Authorization;
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
            catch (Exception ex)
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

        [HttpGet("unmapped-characters")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UnmappedCharacter>>> GetUnmappedCharacters()
        {
            try
            {
                var unmappedCharacters = await _mongoService.GetUnmappedCharacters();
                return Ok(unmappedCharacters);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching unmapped characters: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching unmapped characters");
            }
        }

        [HttpGet("unmapped-discord-users")]
        [Authorize]
        public async Task<ActionResult<GetUnmappedDiscordUsersResponse>> GetUnmappedDiscordUsers([FromQuery] string characterName)
        {
            try
            {
                if (string.IsNullOrEmpty(characterName))
                {
                    return BadRequest("characterName is required");
                }

                if(characterName.Contains(' ') == false)
                {
                    return BadRequest("characterName must contain a space");
                }

                var unmappedDiscordUsersResponse = await _mongoService.GetUnmappedDiscordUsersForCharacter(characterName);
                return Ok(unmappedDiscordUsersResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching unmapped discord users: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching unmapped discord users");
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

    public class UnmappedCharacter
    {
        public string CharacterId { get; set; }
        public string Name { get; set; }
    }

    public class GetUnmappedDiscordUsersResponse
    {
        public IEnumerable<UnmappedDiscordUser> SuggestedDiscordUsers { get; set; }
        public IEnumerable<UnmappedDiscordUser> UnmappedDiscordUsers { get; set; }
    }

    public class UnmappedDiscordUser
    {
        public string DiscordId { get; set; }
        public string ServerNickName { get; set; }
    }
}

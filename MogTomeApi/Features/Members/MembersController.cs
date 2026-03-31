using MediatR;
using Microsoft.AspNetCore.Mvc;
using MogTomeApi.Features.Members.Queries;

namespace MogTomeApi.Features.Members
{
    [ApiController]
    [Route("members")]
    public class MembersController : ControllerBase
    {
        private readonly ILogger<MembersController> _logger;
        private readonly IMediator _mediator;

        public MembersController(ILogger<MembersController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet()]
        public async Task<ActionResult> GetMembers()
        {
            try
            {
                var result = await _mediator.Send(new GetMembersQuery());

                if (result.IsSuccess)
                {
                    return Ok(result.Value);

                }
                else
                {
                    _logger.LogError("Failed get members: {Message}", result.Error);
                    return StatusCode((int)result.StatusCode, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve members: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when retrieving members");
            }
        }

        [HttpGet("staff")]
        public async Task<ActionResult> GetStaff()
        {
            try
            {
                var result = await _mediator.Send(new GetStaffMembersQuery());
                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching staff: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred when fetching staff");
            }
        }
    }
}

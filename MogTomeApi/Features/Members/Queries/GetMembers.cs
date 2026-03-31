using MediatR;
using MogTomeApi.Features.Members.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Members.Queries
{
    public class GetMembersQuery : IRequest<Result<GetMembersResponse>> { }

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

    public class GetMembersHandler : IRequestHandler<GetMembersQuery, Result<GetMembersResponse>>
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ILogger<GetMembersHandler> _logger;

        public GetMembersHandler(IMongoDatabase mongoDatabase, ILogger<GetMembersHandler> logger)
        {
            _mongoDatabase = mongoDatabase;
            _logger = logger;
        }

        public async Task<Result<GetMembersResponse>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var memberCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");
                var filter = Builders<FreeCompanyMember>.Filter.Empty;
                var freeCompanyMembers = await memberCollection
                    .Find(filter)
                    .ToListAsync();

                var activeMembers = freeCompanyMembers.Where(member => member.ActiveMember).ToList();
                var memberResponse = activeMembers.Select(member => new Member
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
                    TotalCount = activeMembers.Count
                };

                return new Result<GetMembersResponse>(response, HttpStatusCode.OK, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching members: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return new Result<GetMembersResponse>(null, HttpStatusCode.InternalServerError, false, "An error occurred when fetching members");
            }
        }
    }
}

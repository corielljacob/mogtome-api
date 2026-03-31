using MediatR;
using MogTomeApi.Features.Members.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Members.Queries
{
    public class GetStaffMembersQuery : IRequest<Result<GetStaffMembersResponse>> { }

    public class GetStaffMembersResponse
    {
        public int TotalCount { get; set; }
        public required IEnumerable<FreeCompanyStaffMember> Staff { get; set; }
    }

    public class StaffMember
    {
        public string Name { get; set; }
        public string FreeCompanyRank { get; set; }
        public string FreeCompanyRankIcon { get; set; }
        public string CharacterId { get; set; }
        public bool ActiveMember { get; set; }
        public string AvatarLink { get; set; }
    }

    public class GetStaffMembersHandler : IRequestHandler<GetStaffMembersQuery, Result<GetStaffMembersResponse>>
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ILogger<GetStaffMembersHandler> _logger;

        public GetStaffMembersHandler(IMongoDatabase mongoDatabase, ILogger<GetStaffMembersHandler> logger)
        {
            _mongoDatabase = mongoDatabase;
            _logger = logger;
        }

        public async Task<Result<GetStaffMembersResponse>> Handle(GetStaffMembersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var memberCollection = _mongoDatabase.GetCollection<FreeCompanyStaffMember>("members");

                var filter = Builders<FreeCompanyStaffMember>.Filter.And(
                    Builders<FreeCompanyStaffMember>.Filter.Or(
                        Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.MoogleKnight),
                        Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.PaissaTrainer),
                        Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.MoogleGuardian)
                    ),
                    Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.ActiveMember, true),
                    Builders<FreeCompanyStaffMember>.Filter.Ne(member => member.Name, Constants.PassiveToast)
                );

                var staff = await memberCollection
                    .Find(filter)
                    .ToListAsync(cancellationToken);

                var response = new GetStaffMembersResponse
                {
                    Staff = staff,
                    TotalCount = staff.Count
                };

                return new Result<GetStaffMembersResponse>(response, HttpStatusCode.OK, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching members: {Message}\nStack Trace:{Trace}", ex.Message, ex.StackTrace);
                return new Result<GetStaffMembersResponse>(null, HttpStatusCode.InternalServerError, false, "An error occurred when fetching members");
            }
        }
    }
}

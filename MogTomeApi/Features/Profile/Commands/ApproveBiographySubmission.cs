using MediatR;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Profile.Commands
{
    public class ApproveBiographySubmission
    {
        public class Command : IRequest<Result<ApproveResult>>
        {
            public Guid SubmissionId { get; set; }
            public string ApprovedByDiscordId { get; set; }
        }

        public class ApproveResult { }

        public class Handler : IRequestHandler<Command, Result<ApproveResult>>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(ILogger<Handler> logger, IMongoDatabase mongoDatabase)
            {
                _logger = logger;
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<ApproveResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                var submissionCollection = _mongoDatabase.GetCollection<BiographySubmission>("biography-submissions");
                var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmissionId, request.SubmissionId);

                var submission = await submissionCollection
                    .Find(filter)
                    .SingleOrDefaultAsync(cancellationToken);

                if (submission == null)
                {
                    return new Result<ApproveResult>(null, HttpStatusCode.NotFound, true, "The requested submission could not be found.");
                }

                await SetUserBiography(submission.SubmittedByDiscordId, submission.Biography);

                var update = Builders<BiographySubmission>.Update
                    .Set(submission => submission.Status, "Approved")
                    .Set(submission => submission.ApprovedByDiscordId, request.ApprovedByDiscordId);

                await submissionCollection.UpdateOneAsync(filter, update);

                return new Result<ApproveResult>(null, HttpStatusCode.OK, true, null);
            }

            private async Task SetUserBiography(string discordId, string biography)
            {
                var memberCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");
                var filter = Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId);

                var update = Builders<FreeCompanyMember>.Update
                    .Set(member => member.Biography, biography);

                await memberCollection.UpdateOneAsync(filter, update);
            }
        }
    }
}

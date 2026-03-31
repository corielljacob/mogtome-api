using MediatR;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Profile.Commands
{
    public class RejectBiographySubmission
    {
        public class Command : IRequest<Result<RejectResult>>
        {
            public Guid SubmissionId { get; set; }
            public string RejectedByDiscordId { get; set; }
        }

        public class RejectResult { }

        public class Handler : IRequestHandler<Command, Result<RejectResult>>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(ILogger<Handler> logger, IMongoDatabase mongoDatabase)
            {
                _logger = logger;
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<RejectResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                var submissionCollection = _mongoDatabase.GetCollection<BiographySubmission>("biography-submissions");
                var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmissionId, request.SubmissionId);

                var submission = await submissionCollection
                    .Find(filter)
                    .SingleOrDefaultAsync(cancellationToken);

                if (submission == null)
                {
                    return new Result<RejectResult>(null, HttpStatusCode.NotFound, true, "The requested submission could not be found.");
                }

                var update = Builders<BiographySubmission>.Update
                    .Set(submission => submission.Status, "Rejected")
                    .Set(submission => submission.RejectedByDiscordId, request.RejectedByDiscordId);

                await submissionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

                return new Result<RejectResult>(null, HttpStatusCode.OK, true, null);
            }
        }
    }
}

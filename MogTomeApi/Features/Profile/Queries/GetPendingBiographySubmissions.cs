using MediatR;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Shared;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Profile.Queries
{
    public class GetPendingBiographySubmissions
    {
        public class Query : IRequest<Result<IEnumerable<Submission>>> { }

        public class Submission 
        {
            public ObjectId Id { get; set; }
            public Guid SubmissionId { get; set; }
            public string SubmittedByDiscordId { get; set; }
            public string Biography { get; set; }
            public string Status { get; set; }
            public DateTime SubmittedAt { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<IEnumerable<Submission>>>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(ILogger<Handler> logger, IMongoDatabase mongoDatabase)
            {
                _logger = logger;
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<IEnumerable<Submission>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var submissionCollection = _mongoDatabase.GetCollection<BiographySubmission>("biography-submissions");
                var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.Status, "Pending");

                var submissions = (await submissionCollection
                    .Find(filter)
                    .ToListAsync(cancellationToken))
                    .Select(submission => new Submission
                    {
                        Id = submission.Id,
                        SubmissionId = submission.SubmissionId,
                        SubmittedByDiscordId = submission.SubmittedByDiscordId,
                        Biography = submission.Biography,
                        Status = submission.Status,
                        SubmittedAt = submission.SubmittedAt
                    });

                return new Result<IEnumerable<Submission>>(submissions, HttpStatusCode.OK, true, null);
            }
        }
    }
}

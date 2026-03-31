using MediatR;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Shared;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Profile.Queries
{
    public class GetSubmissionForMember
    {
        public class Query : IRequest<Result<Submission>> 
        { 
            public string DiscordId { get; set; }
        }

        public class Submission 
        {
            public ObjectId Id { get; set; }
            public Guid SubmissionId { get; set; }
            public string SubmittedByDiscordId { get; set; }
            public string Biography { get; set; }
            public string Status { get; set; }
            public DateTime SubmittedAt { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<Submission>>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(ILogger<Handler> logger, IMongoDatabase mongoDatabase)
            {
                _logger = logger;
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<Submission>> Handle(Query request, CancellationToken cancellationToken)
            {
                var submissionCollection = _mongoDatabase.GetCollection<BiographySubmission>("biography-submissions");
                var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmittedByDiscordId, request.DiscordId);

                var submissions = await submissionCollection
                    .Find(filter)
                    .ToListAsync(cancellationToken);

                if (submissions.Any(submission => submission.Status == "Pending") == false && submissions.Any(submission => submission.Status == "Approved") == false)
                {
                    return new Result<Submission>(null, HttpStatusCode.NoContent, true, null);
                }

                BiographySubmission submission;

                if (submissions.Any(submission => submission.Status == "Pending") == false)
                {
                    submission = submissions
                        .Where(submission => submission.Status == "Approved")
                        .OrderByDescending(submission => submission.SubmittedAt)
                        .FirstOrDefault();
                }
                else
                {
                    submission = submissions
                        .Where(submission => submission.Status == "Pending")
                        .OrderByDescending(submission => submission.SubmittedAt)
                        .FirstOrDefault();
                }

                var result = new Submission
                {
                    Id = submission.Id,
                    SubmissionId = submission.SubmissionId,
                    SubmittedByDiscordId = submission.SubmittedByDiscordId,
                    Biography = submission.Biography,
                    Status = submission.Status,
                    SubmittedAt = submission.SubmittedAt
                };

                return new Result<Submission>(result, HttpStatusCode.OK, true, null);
            }
        }
    }
}

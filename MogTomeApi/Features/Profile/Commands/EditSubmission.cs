using MediatR;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Profile.Commands
{
    public class EditSubmission
    {
        public class Command : IRequest<Result<EditResult>>
        {
            public Guid SubmissionId { get; set; }
            public string Biography { get; set; }
            public string DiscordId { get; set; }
        }

        public class EditResult { }

        public class Handler : IRequestHandler<Command, Result<EditResult>>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(ILogger<Handler> logger, IMongoDatabase mongoDatabase)
            {
                _logger = logger;
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<EditResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                var submissionCollection = _mongoDatabase.GetCollection<BiographySubmission>("biography-submissions");

                var filter = Builders<BiographySubmission>.Filter.And(
                    Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmissionId, request.SubmissionId),
                    Builders<BiographySubmission>.Filter.Eq(submission => submission.Status, "Pending")
                );

                var submission = await submissionCollection
                    .Find(filter)
                    .SingleOrDefaultAsync(cancellationToken);

                if (submission == null)
                {
                    return new Result<EditResult>(null, HttpStatusCode.NotFound, false, "The requested submission could not be found");
                }

                var update = Builders<BiographySubmission>.Update
                    .Set(member => member.Biography, request.Biography);

                await submissionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

                return new Result<EditResult>(null, HttpStatusCode.OK, true, null);
            }
        }
    }
}

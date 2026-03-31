using MediatR;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Profile.Commands
{
    public class CreateBiographySubmission
    {
        public class Command : IRequest<Result<CreateSubmissionResult>>
        {
            public string Biography { get; set; }
            public string DiscordId { get; set; }
        }

        public class CreateSubmissionResult { }

        public class Handler : IRequestHandler<Command, Result<CreateSubmissionResult>>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(ILogger<Handler> logger, IMongoDatabase mongoDatabase)
            {
                _logger = logger;
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<CreateSubmissionResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                var submissionCollection = _mongoDatabase.GetCollection<BiographySubmission>("biography-submissions");

                var submission = new BiographySubmission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmittedByDiscordId = request.DiscordId,
                    Biography = request.Biography,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                await submissionCollection.InsertOneAsync(submission, cancellationToken: cancellationToken);

                return new Result<CreateSubmissionResult>(null, HttpStatusCode.OK, true, null);
            }
        }
    }
}

using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MogTomeApi.Features.Profile.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Profile.Commands
{
    public class SetBiography
    {
        public class Command : IRequest<Result<SetResult>>
        {
            public string Biography { get; set; }
            [BindNever]
            public string DiscordId { get; set; }
        }

        public class SetResult { }

        public class Handler : IRequestHandler<Command, Result<SetResult>>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(ILogger<Handler> logger, IMongoDatabase mongoDatabase)
            {
                _logger = logger;
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<SetResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                var membersCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");
                var filter = Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, request.DiscordId);

                var update = Builders<FreeCompanyMember>.Update
                    .Set(member => member.Biography, request.Biography);

                await membersCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

                return new Result<SetResult>(null, HttpStatusCode.OK, true, null);
            }
        }
    }
}

using MediatR;
using MogTomeApi.Features.Dashboard.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Dashboard 
{
    public class MapDiscordAccountToCharacter
    {
        public class Command : IRequest<Result<MapResult>>
        {
            public string CharacterId { get; set; }
            public string DiscordId { get; set; }
        }

        public class MapResult
        {
            public bool MappingSuccessful { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<MapResult>>
        {
            private readonly IMongoDatabase _mongoDatabase;
            private readonly ILogger<Handler> _logger;

            public Handler(IMongoDatabase mongoDatabase, ILogger<Handler> logger)
            {
                _mongoDatabase = mongoDatabase;
                _logger = logger;
            }

            public async Task<Result<MapResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                var membersCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");
                var filter = Builders<FreeCompanyMember>.Filter.Eq(member => member.CharacterId, request.CharacterId);

                var update = Builders<FreeCompanyMember>.Update
                    .Set(member => member.DiscordId, request.DiscordId);

                var updateResult = await membersCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

                if (updateResult.ModifiedCount == 1)
                {
                    var result = new MapResult
                    {
                        MappingSuccessful = true
                    };

                    return new Result<MapResult>(result, HttpStatusCode.OK, true, null);
                }
                else
                {
                    _logger.LogError("Unable to update member with character id {characterId} and discordId {discordId}", request.CharacterId, request.DiscordId);
                    return new Result<MapResult>(null, HttpStatusCode.InternalServerError, false, "Unable to update member with character id");
                }
            }
        }
    }
}

using MediatR;
using MogTomeApi.Features.Dashboard.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Dashboard.Commands 
{
    public class UnlinkDiscordAccountFromCharacter
    {
        public class UnlinkDiscordAccountFromCharacterCommand : IRequest<Result<UnlinkResult>>
        {
            public string CharacterId { get; set; }
            public string DiscordId { get; set; }
        }

        public class UnlinkResult
        {
            public bool UnlinkSuccessful { get; set; }
        }

        public class Handler : IRequestHandler<UnlinkDiscordAccountFromCharacterCommand, Result<UnlinkResult>>
        {
            private readonly IMongoDatabase _mongoDatabase;
            private readonly ILogger<Handler> _logger;

            public Handler(IMongoDatabase mongoDatabase, ILogger<Handler> logger)
            {
                _mongoDatabase = mongoDatabase;
                _logger = logger;
            }

            public async Task<Result<UnlinkResult>> Handle(UnlinkDiscordAccountFromCharacterCommand request, CancellationToken cancellationToken)
            {
                var membersCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");
                var filter = Builders<FreeCompanyMember>.Filter.Eq(member => member.CharacterId, request.CharacterId);

                var update = Builders<FreeCompanyMember>.Update
                    .Set(member => member.DiscordId, null);

                var updateResult = await membersCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

                if (updateResult.ModifiedCount == 1)
                {
                    var result = new UnlinkResult
                    {
                        UnlinkSuccessful = true
                    };

                    return new Result<UnlinkResult>(result, HttpStatusCode.OK, true, null);
                }
                else
                {
                    _logger.LogError("Unable to unlink member for character id {characterId} and discordId {discordId}", request.CharacterId, request.DiscordId);
                    return new Result<UnlinkResult>(null, HttpStatusCode.InternalServerError, false, "Unable to unlink member for character");
                }
            }
        }
    }
}

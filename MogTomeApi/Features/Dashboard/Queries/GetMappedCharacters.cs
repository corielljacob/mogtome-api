using MediatR;
using MogTomeApi.Features.Dashboard.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Dashboard.Queries
{
    public class GetMappedCharactersQuery : IRequest<Result<GetMappedCharactersResponse>> { }

    public class GetMappedCharactersResponse
    {
        public int TotalCount { get; set; }
        public required IEnumerable<MappedCharacter> MappedCharacters { get; set; }
    }

    public class MappedCharacter
    {
        public string CharacterId { get; set; }
        public string CharacterName { get; set; }
        public string DiscordName { get; set; }
        public string DiscordId { get; set; }
    }

    public class GetMappedCharactersHandler : IRequestHandler<GetMappedCharactersQuery, Result<GetMappedCharactersResponse>>
    {
        private readonly IMongoDatabase _mongoDatabase;

        public GetMappedCharactersHandler(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
        }

        public async Task<Result<GetMappedCharactersResponse>> Handle(GetMappedCharactersQuery request, CancellationToken cancellationToken)
        {
            var memberCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");

            var filter = Builders<FreeCompanyMember>.Filter.And(
                Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true),
                Builders<FreeCompanyMember>.Filter.Where(member => string.IsNullOrEmpty(member.DiscordId) == false)
            );

            var mappedCharacters = (await memberCollection
                .Find(filter)
                .ToListAsync(cancellationToken))
                .OrderBy(character => character.Name)
                .Join(_mongoDatabase.GetCollection<DiscordMember>("discord-members").Find(Builders<DiscordMember>.Filter.Empty).ToList(cancellationToken), 
                    character => character.DiscordId, 
                    discordUser => discordUser.DiscordId, 
                    (character, discordUser) => new { character, discordUser })
                .Select(mapping => new MappedCharacter
                {
                    CharacterId = mapping.character.CharacterId,
                    CharacterName = mapping.character.Name,
                    DiscordName = mapping.discordUser.Name,
                    DiscordId = mapping.character.DiscordId
                });

            var response = new GetMappedCharactersResponse
            {
                TotalCount = mappedCharacters.Count(),
                MappedCharacters = mappedCharacters
            };

            return new Result<GetMappedCharactersResponse>(response, HttpStatusCode.OK, true, null);
        }
    }
}

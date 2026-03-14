using F23.StringSimilarity;
using MediatR;
using MogTomeApi.Features.Dashboard.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;
using System.Text.RegularExpressions;
using static MogTomeApi.Features.Dashboard.GetUnmappedDiscordUsers;

namespace MogTomeApi.Features.Dashboard 
{
    public class GetUnmappedCharacters 
    {
        public class Query : IRequest<Result<GetUnmappedDiscordUsersResponse>>
        {
            public string DiscordUsername { get; set; }
        }

        public class GetUnmappedDiscordUsersResponse
        {
            public IEnumerable<UnmappedCharacter> SuggestedCharacters { get; set; }
            public IEnumerable<UnmappedCharacter> UnmappedCharacters { get; set; }
        }

        public class UnmappedCharacter 
        {
            public string CharacterId { get; set; }
            public string Name { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<GetUnmappedDiscordUsersResponse>>
        {
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(IMongoDatabase mongoDatabase)
            {
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<GetUnmappedDiscordUsersResponse>> Handle(Query request, CancellationToken cancellationToken)
            {
                var memberCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");

                var filter = Builders<FreeCompanyMember>.Filter.And(
                    Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true),
                    Builders<FreeCompanyMember>.Filter.Where(member => string.IsNullOrEmpty(member.DiscordId))
                );

                var unmappedCharacters = (await memberCollection
                    .Find(filter)
                    .ToListAsync(cancellationToken))
                    .OrderBy(character => character.Name)
                    .Select(character => new UnmappedCharacter
                    {
                        CharacterId = character.CharacterId,
                        Name = character.Name
                    });

                List<UnmappedCharacter> suggestedCharacters = [];
                if (string.IsNullOrEmpty(request.DiscordUsername) == false)
                    PopulateSuggestedCharacterNames(unmappedCharacters, suggestedCharacters, request.DiscordUsername);

                var result = new GetUnmappedDiscordUsersResponse
                {
                    SuggestedCharacters = suggestedCharacters,
                    UnmappedCharacters = unmappedCharacters
                };

                return new Result<GetUnmappedDiscordUsersResponse>(result, HttpStatusCode.OK, true, null);
            }

            private static void PopulateSuggestedCharacterNames(IEnumerable<UnmappedCharacter> unmappedCharacters, List<UnmappedCharacter> suggestedCharacters, string discordUsername)
            {
                var stringComparer = new NormalizedLevenshtein();

                foreach (var character in unmappedCharacters)
                {
                    discordUsername = Regex.Replace(discordUsername, "[^A-Za-z ]", "");
                    var discordUsernameSplits = discordUsername.Split(" ");

                    foreach(var split in discordUsernameSplits)
                    {
                        var characterSplitName = character.Name.Split(" ");
                        var firstNameSimilarity = stringComparer.Distance(characterSplitName[0], split);
                        var lastNameSimilarity = stringComparer.Distance(characterSplitName[1], split);

                        if (firstNameSimilarity <= 0.25 || lastNameSimilarity <= 0.25 || character.Name.Contains(split, StringComparison.OrdinalIgnoreCase))
                        {
                            var suggestion = new UnmappedCharacter
                            {
                                CharacterId = character.CharacterId,
                                Name = character.Name
                            };

                            suggestedCharacters.Add(suggestion);
                            break;
                        }
                    }
                }

                suggestedCharacters = suggestedCharacters.OrderBy(member => member.Name).ToList();
            }
        }
    }
}
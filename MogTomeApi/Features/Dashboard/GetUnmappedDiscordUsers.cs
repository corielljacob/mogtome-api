using F23.StringSimilarity;
using MediatR;
using MogTomeApi.Features.Dashboard.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Dashboard 
{
    public class GetUnmappedDiscordUsers
    {
        public class Query : IRequest<Result<GetUnmappedDiscordUsersResponse>>
        {
            public string CharacterName { get; set; }
        }

        public class GetUnmappedDiscordUsersResponse {
            public IEnumerable<UnmappedDiscordUser> SuggestedDiscordUsers { get; set; }
            public IEnumerable<UnmappedDiscordUser> UnmappedDiscordUsers { get; set; }
        }

        public class UnmappedDiscordUser {
            public string DiscordId { get; set; }
            public string ServerNickName { get; set; }
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
                var discordMembers = await GetAllDiscordMembers();
                var mappedMemberIds = await GetMappedMemberDiscordIds();

                List<UnmappedDiscordUser> unmappedDiscordUsers = discordMembers
                    .Select(member => new UnmappedDiscordUser
                    {
                        DiscordId = member.DiscordId,
                        ServerNickName = member.Name
                    })
                    .Where(member => mappedMemberIds.Contains(member.DiscordId) == false)
                    .OrderBy(member => member.ServerNickName)
                    .ToList();

                List<UnmappedDiscordUser> suggestedDiscordUsers = [];
                if (string.IsNullOrEmpty(request.CharacterName) == false)
                    PopulateSuggestedDiscordMembers(discordMembers, suggestedDiscordUsers, request.CharacterName);

                var unmappedDiscordUsersResponse = new GetUnmappedDiscordUsersResponse
                {
                    SuggestedDiscordUsers = suggestedDiscordUsers,
                    UnmappedDiscordUsers = unmappedDiscordUsers
                };

                return new Result<GetUnmappedDiscordUsersResponse>(unmappedDiscordUsersResponse, HttpStatusCode.OK, true, null);
            }

            private async Task<IEnumerable<string>> GetMappedMemberDiscordIds()
            {
                var memberCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");
                var mappedMemberFilter = Builders<FreeCompanyMember>.Filter.And(
                    Builders<FreeCompanyMember>.Filter.Where(member => string.IsNullOrEmpty(member.DiscordId) == false),
                    Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true)
                );

                var mappedMembers = await memberCollection
                    .Find(mappedMemberFilter)
                    .ToListAsync();

                var mappedMemberIds = mappedMembers.Select(member => member.DiscordId);
                return mappedMemberIds;
            }

            private async Task<IEnumerable<DiscordMember>> GetAllDiscordMembers()
            {
                var discordMemberCollection = _mongoDatabase.GetCollection<DiscordMember>("discord-members");
                var discordMemberFilter = Builders<DiscordMember>.Filter.Empty;

                var discordMembers = await discordMemberCollection
                    .Find(discordMemberFilter)
                    .ToListAsync();

                return discordMembers;
            }

            private static void PopulateSuggestedDiscordMembers(IEnumerable<DiscordMember> discordMembers, List<UnmappedDiscordUser> suggestedDiscordUsers, string characterName)
            {
                var stringComparer = new NormalizedLevenshtein();

                foreach (var member in discordMembers)
                {
                    var characterNameSplit = characterName.Split(" ");
                    var firstName = characterNameSplit[0];
                    var similarity = stringComparer.Distance(member.Name, characterName);
                    if (similarity <= 0.25 || member.Name.Contains(firstName, StringComparison.OrdinalIgnoreCase))
                    {
                        var suggestion = new UnmappedDiscordUser
                        {
                            DiscordId = member.DiscordId,
                            ServerNickName = member.Name
                        };

                        suggestedDiscordUsers.Add(suggestion);
                    }
                }

                suggestedDiscordUsers = suggestedDiscordUsers.OrderBy(member => member.ServerNickName).ToList();
            }
        }
    }
}